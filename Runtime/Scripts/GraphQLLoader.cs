namespace rlmg.Tools.ContentLoading
{
    using System;
    using System.Collections;
    using System.IO;
    using Newtonsoft.Json;
    using UnityEngine;
    using UnityEngine.Networking;

    /// <summary>
    /// ContentLoader dedicated to loading content from a GraphQL endpoint, with the option to load query text and config settings from disk as well.
    /// </summary>
    /// <remarks>The REST endpoint can be configured separately, allowing for flexibility in specifying different root URLs for REST and GraphQL requests. The REST request is intended to be used more or less interchangeably with the GraphQL requests, as a debugging tool.</remarks>
    [RequireComponent(typeof(ContentCacher))]
    public class GraphQLLoader : ContentLoader
    {

        [Serializable]
        protected enum RequestMode
        {
            GraphQL = 0,
            REST_GET = 1,
        }

        [Header("GraphQL Settings")]
        /// <summary>
        /// Whether to use the local config file as the source for serverURL, graphEndpoint, assetsEndpoint, authToken, operationName, etc.
        /// </summary>
        [SerializeField]
        [Tooltip("Whether to use the local config file as the source for userServer, doCache, serverURL, etc.")]
        protected bool doLoadLoaderConfigFromDisk = true;

        /// <summary>
        /// File name of this loader's config file, which contains serverURL, graphEndpoint, assetsEndpoint, authToken, operationName, etc.
        /// </summary>
        [SerializeField] protected string loaderConfigFileName = "server_config.json";

        /// <summary>
        /// Used as the loader config if loading localLoaderConfigFilePath from disk fails (or fails to
        /// parse) and this is assigned.
        /// </summary>
        [SerializeField] protected TextAsset fallbackConfigTextAsset;

        /// <summary>
        /// The file path to this loader's config file, which contains serverURL, graphEndpoint, assetsEndpoint, authToken, operationName, etc.
        /// </summary>
        protected virtual string localLoaderConfigFilePath
        {
            get
            {
                return Path.Combine(
                        LocalContentDirectory,
                        loaderConfigFileName);
            }
        }

        [Header("GraphQL Settings - Query - Configurable by Config File")]
        /// <summary>
        /// The mode of the request to be made.
        /// </summary>
        [SerializeField] protected RequestMode requestMode = RequestMode.GraphQL;

        /// <summary>
        /// Whether to use the local query text file as the source for the query.
        /// </summary>
        [SerializeField] protected bool doLoadQueryFromDisk = true;

        /// <summary>
        /// File name of GraphQL-formatted query text doc
        /// Assumed to be located in localContentDirectory
        /// </summary>
        [SerializeField]
        protected string queryFileName = "query.txt";

        /// <summary>
        /// Used as the query text if loading localQueryFilePath from disk fails and this is assigned.
        /// </summary>
        [SerializeField] protected TextAsset fallbackQueryTextAsset;

        /// <summary>
        /// The file path to the query text doc
        /// </summary>
        protected virtual string localQueryFilePath
        {
            get
            {
                return Path.Combine(
                        LocalContentDirectory,
                        queryFileName);
            }
        }

        /// <summary>
        /// Optional. Used if doLoadQueryFromDisk is false.
        /// </summary>
        [Multiline]
        [SerializeField] protected string queryText;

        [Header("GraphQL Settings - Request - Configurable by Config File")]
        /// <summary>
        /// Whether to attempt to load content from the specified server.
        /// If false, will still attempt to load content from disk at localContentPath.
        /// </summary>
        public bool UseServer = true;

        /// <summary>
        /// Root URL for graph and assets server.
        /// </summary>
        [SerializeField] protected string serverURL;

        /// <summary>
        /// Endpoint where GraphQL can be queried.
        /// </summary>
        [SerializeField] protected string graphEndpoint = "graphql";

        /// <summary>
        /// URL for GraphQL queries.
        /// </summary>
        protected virtual string graphURL
        {
            get
            {
                return serverURL + "/" + graphEndpoint;
            }
        }

        /// <summary>
        /// Endpoint from where hashed assets can be REST downloaded.
        /// </summary>
        [SerializeField] protected string assetsEndpoint = "assets";

        /// <summary>
        /// URL for asset REST requests.
        /// </summary>
        public virtual string AssetsURL
        {
            get
            {
                return serverURL + "/" + assetsEndpoint;
            }
        }

        /// <summary>
        /// Endpoint for REST requests that return json data.
        /// Entries that don't start with "http" will be appended to serverURL to make the full URL for the request, but if restURLOrEndpoint starts with "http", it will be used as the full URL for the request. This allows for more flexibility in specifying the REST endpoint, since some servers might have a different root URL for REST requests than for GraphQL requests.
        /// </summary>
        [SerializeField] protected string restURLOrEndpoint = null;

        /// <summary>
        /// URL for REST requests that return json data
        /// </summary>
        protected virtual string restURL
        {
            get
            {
                if (restURLOrEndpoint.StartsWith("http"))
                    return restURLOrEndpoint;

                return serverURL + "/" + restURLOrEndpoint;
            }
        }

        /// <summary>
        /// Name of the environment variable to read the auth token from at runtime. Recommended over
        /// authToken: the token is never written to a scene, prefab, or config file on disk. See
        /// <see cref="GetAuthToken"/>.
        /// </summary>
        [SerializeField] protected string authTokenEnvironmentVariable = "RLMG_GRAPHQL_AUTH_TOKEN";

        /// <summary>
        /// Used only if authTokenEnvironmentVariable is blank, or unset in the environment. Avoid
        /// setting this to a real token for anything other than local testing - unlike the environment
        /// variable, it gets serialized into the scene/prefab (or server_config.json) in plain text.
        /// </summary>
        [SerializeField] protected string authToken;

        /// <summary>
        /// The operation name to post with the query.
        /// </summary>
        [SerializeField] protected string operationName;

        [Header("Retry")]
        /// <summary>
        /// Whether a failed remote request is retried (with backoff) before falling back to local content.
        /// </summary>
        [SerializeField] protected bool autoRetryFailedRequests = true;

        /// <summary>Maximum retry attempts before falling back to local content. 0 = unlimited.</summary>
        [SerializeField] protected int maxRetryAttempts = 3;

        /// <summary>Delay before the first retry, in seconds.</summary>
        [SerializeField] protected float initialRetryBackoffSeconds = 1f;

        /// <summary>Upper bound on retry delay, in seconds.</summary>
        [SerializeField] protected float maxRetryBackoffSeconds = 15f;

        /// <summary>Multiplier applied to the retry delay after each attempt.</summary>
        [SerializeField] protected float retryBackoffMultiplier = 2f;

        /// <summary>
        /// Manager class for caching.
        /// </summary>
        protected ContentCacher cacher;

        [Header("Events")]
        /// <summary>
        /// If either the config loader or query loader failed.
        /// </summary>
        public UnityEngine.Events.UnityEvent<string> AnySupportLoadFailed;

        /// <summary>
        /// Invoked when a failed remote request is about to be retried, with the attempt number (1-based).
        /// </summary>
        public UnityEngine.Events.UnityEvent<int> RetryingRequest;

        /// <summary>
        /// Invoked when a remote request attempt fails but will be retried, with the failed
        /// UnityWebRequest. Fires before <see cref="RetryingRequest"/>, and only when a retry is
        /// actually going to happen (i.e. not on the final, fatal failure).
        /// </summary>
        public UnityEngine.Events.UnityEvent<UnityWebRequest> AttemptFailedButWillRetry;


        #region Graphql Loading
        /// <summary>
        /// Override the default load method with graphql-specific logic
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator MainLoadContent()
        {
            yield return LoadRemoteContent();
        }

        /// <summary>
        /// Serializable class for converting data to json
        /// </summary>
        [System.Serializable]
        public class GraphPostData
        {
            public string query;
            public object variables;

            public GraphPostData(string _query, object _variables)
            {
                query = _query;
                variables = _variables; //e.g. object variables = new { erasByIdId = erasByIdIdVariable };
            }
        }

        protected override void Awake()
        {
            base.Awake();
            cacher = GetComponent<ContentCacher>();
        }

        #region GraphQL Loading Steps
        /// <summary>
        /// Load the loader config from disk, which may contain settings for serverURL, graphEndpoint, assetsEndpoint, authToken, operationName, etc.
        /// Falls back to <see cref="fallbackConfigTextAsset"/> (if assigned) when the on-disk file fails
        /// to load or parse, writing it to disk so a valid file exists there next time.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator LoadLoaderConfig()
        {
            if (!doLoadLoaderConfigFromDisk)
                yield break;

            yield return FileLoadingUtility.LoadJsonWithFallbackCoroutine<CMSClientConfigData>(
                localLoaderConfigFilePath,
                fallbackConfigTextAsset,
                "loader config file",
                ApplyConfigSettings,
                message =>
                {
                    Debug.LogError(message);
                    AnySupportLoadFailed?.Invoke(message);
                });
        }

        /// <summary>
        /// Goes through config settings and applies them to this MonoBehaviour
        /// </summary>
        /// <param name="configData"></param>
        protected void ApplyConfigSettings(CMSClientConfigData configData)
        {
            if (configData == null)
                return;

            if (!configData.useThisConfig)
                return;

            UseServer = configData.useServer;
            cacher.DoCacheText = configData.doCache;

            if (configData.requestMode != RequestModeOption.NoOverride)
                requestMode = (RequestMode)configData.requestMode;

            if (configData.contentLocationRoot != ContentLocationRootOption.NoOverride)
                contentLocationRoot = (ContentLocationRoot)configData.contentLocationRoot;

            if (configData.contentDirName != null)
                ContentDirName = configData.contentDirName;

            if (configData.contentFileName != null)
                contentFileName = configData.contentFileName;

            if (configData.serverURL != null)
                serverURL = configData.serverURL;

            if (configData.graphEndpoint != null)
                graphEndpoint = configData.graphEndpoint;

            if (configData.assetsEndpoint != null)
                assetsEndpoint = configData.assetsEndpoint;

            if (configData.restEndpoint != null)
                restURLOrEndpoint = configData.restEndpoint;

            if (configData.authTokenEnvironmentVariable != null)
                authTokenEnvironmentVariable = configData.authTokenEnvironmentVariable;

            if (configData.authToken != null)
                authToken = configData.authToken;

            if (configData.operationName != null)
                operationName = configData.operationName;

            if (configData.queryFileName != null)
                queryFileName = configData.queryFileName;

            if (configData.autoRetryFailedRequests.HasValue)
                autoRetryFailedRequests = configData.autoRetryFailedRequests.Value;

            if (configData.maxRetryAttempts.HasValue)
                maxRetryAttempts = configData.maxRetryAttempts.Value;

            if (configData.initialRetryBackoffSeconds.HasValue)
                initialRetryBackoffSeconds = configData.initialRetryBackoffSeconds.Value;

            if (configData.maxRetryBackoffSeconds.HasValue)
                maxRetryBackoffSeconds = configData.maxRetryBackoffSeconds.Value;

            if (configData.retryBackoffMultiplier.HasValue)
                retryBackoffMultiplier = configData.retryBackoffMultiplier.Value;
        }

        /// <summary>
        /// Resolves the auth token to send with requests: <see cref="authTokenEnvironmentVariable"/>
        /// (if set and present in the environment) takes priority, falling back to
        /// <see cref="authToken"/>. Override to source the token from elsewhere - just never log or
        /// serialize the returned value.
        /// </summary>
        protected virtual string GetAuthToken()
        {
            if (!string.IsNullOrEmpty(authTokenEnvironmentVariable))
            {
                string fromEnvironment = Environment.GetEnvironmentVariable(authTokenEnvironmentVariable);
                if (!string.IsNullOrEmpty(fromEnvironment))
                    return fromEnvironment;
            }

            return authToken;
        }

        /// <summary>
        /// Variables to send with the GraphQL query. Override to supply real variables - e.g. an
        /// anonymous object, a Dictionary&lt;string, object&gt;, or a Newtonsoft.Json.Linq.JObject.
        /// </summary>
        protected virtual object GetGraphQLVariables()
        {
            return new { };
        }

        /// <summary>
        /// Load query file from disk. Falls back to <see cref="fallbackQueryTextAsset"/> (if assigned)
        /// when the on-disk file fails to load, writing it to disk so a valid file exists there next time.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator LoadQueryFromDisk()
        {
            if (!doLoadQueryFromDisk)
                yield break;

            yield return FileLoadingUtility.LoadTextWithFallbackCoroutine(
                localQueryFilePath,
                fallbackQueryTextAsset,
                "query file",
                text => queryText = text,
                message =>
                {
                    Debug.LogError(message);
                    AnySupportLoadFailed?.Invoke(message);
                });
        }
        #endregion

        /// <summary>
        /// GraphQL webRequest.
        /// Also loads query file from disk first, if doLoadQueryFromDisk is true.
        /// If the query file fails to load and the query set in the Unity Editor is empty, will call the fatal failure callback and end the loading process, since we can't make the request without the query text.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator DoGraphRequest()
        {
            if (requestMode != RequestMode.GraphQL)
                yield break;

            yield return LoadQueryFromDisk();

            if (string.IsNullOrEmpty(queryText))
            {
                DidLoadSucceed = false;
                yield return OnRemoteFatalFailure(
                    graphURL,
                    "Loaded query body is null or empty."
                );
                AnyLoadFailed?.Invoke(null);
                yield break;
            }

            GraphPostData postData = new GraphPostData(queryText, GetGraphQLVariables());
            string json = JsonConvert.SerializeObject(postData);

            yield return DoRequestWithRetry(() => UnityWebRequest.Post(graphURL, json, "application/json"));
        }

        protected virtual IEnumerator DoRestGetRequest()
        {
            if (requestMode != RequestMode.REST_GET)
                yield break;

            yield return DoRequestWithRetry(() => UnityWebRequest.Get(restURL));
        }

        /// <summary>
        /// Sends a request built by <paramref name="requestFactory"/> (called once per attempt, since a
        /// UnityWebRequest can't be resent), retrying with backoff on failure up to maxRetryAttempts
        /// (0 = unlimited) before falling back to local content.
        /// </summary>
        protected virtual IEnumerator DoRequestWithRetry(Func<UnityWebRequest> requestFactory)
        {
            int attempt = 0;

            while (true)
            {
                float retryDelaySeconds;

                using (UnityWebRequest webRequest = requestFactory())
                {
                    string token = GetAuthToken();
                    if (!string.IsNullOrEmpty(token))
                        webRequest.SetRequestHeader("Authorization", "Bearer " + token);

                    yield return webRequest.SendWebRequest();

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        DidLoadSucceed = true;
                        yield return OnRemoteResponseSuccess(webRequest);
                        yield return AfterAnySuccess(webRequest);
                        AnyLoadSucceeded?.Invoke(webRequest);
                        yield break;
                    }

                    bool canRetry = autoRetryFailedRequests && (maxRetryAttempts <= 0 || attempt < maxRetryAttempts);
                    if (!canRetry)
                    {
                        DidLoadSucceed = false;
                        yield return OnRemoteFatalFailure(webRequest);
                        AnyLoadFailed?.Invoke(webRequest);
                        yield break;
                    }

                    OnAttemptFailedButWillRetry(webRequest);
                    AttemptFailedButWillRetry?.Invoke(webRequest);

                    retryDelaySeconds = Mathf.Min(maxRetryBackoffSeconds, initialRetryBackoffSeconds * Mathf.Pow(retryBackoffMultiplier, attempt));
                }

                attempt++;
                OnRetryingRequest(attempt, retryDelaySeconds);
                RetryingRequest?.Invoke(attempt);

                yield return new WaitForSeconds(retryDelaySeconds);
            }
        }

        /// <summary>A remote request attempt has failed, but a retry will be attempted.</summary>
        protected virtual void OnAttemptFailedButWillRetry(UnityWebRequest webRequest)
        {
            Debug.LogWarning(string.Format(
                "GraphQLLoader: request attempt failed, will retry.\n{0}\n{1}",
                webRequest?.url,
                webRequest?.error));
        }

        /// <summary>A retry has been scheduled after a failed remote request, after the given delay.</summary>
        protected virtual void OnRetryingRequest(int attemptNumber, float delaySeconds)
        {
            Debug.LogWarning(string.Format("GraphQLLoader: Retrying (attempt {0}) in {1:F1}s...", attemptNumber, delaySeconds));
        }

        /// <summary>
        /// First loads the query text file from disk, then
        /// Posts the graphql query and handles response
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator LoadRemoteContent()
        {
            yield return LoadLoaderConfig();

            if (!UseServer)
            {
                // Fall back to local loading, without the fatal error callback
                yield return LoadLocalContent();
                yield break;
            }

            switch (requestMode)
            {
                case RequestMode.GraphQL:
                    yield return DoGraphRequest();
                    break;
                case RequestMode.REST_GET:
                    yield return DoRestGetRequest();
                    break;
                default:
                    Debug.LogError("Unrecognized request mode: " + requestMode);
                    yield break;
            }
        }

        /// <summary>
        /// Callback for graphql request success
        /// </summary>
        /// <param name="webRequest">The successful UnityWebRequest</param>
        /// <returns></returns>
        protected virtual IEnumerator OnRemoteResponseSuccess(UnityWebRequest webRequest)
        {
            if (string.IsNullOrEmpty(webRequest?.downloadHandler?.text)) { yield break; }

            if (cacher.DoCacheText)
                // Only save on remote CMS success, as opposed to saving even when loading locally
                cacher.CacheText(webRequest.downloadHandler.text, localContentPath);
        }

        /// <summary>
        /// Callback for graphql request error
        /// </summary>
        /// <param name="webRequest">The failed request</param>
        /// <returns></returns>
        protected virtual IEnumerator OnRemoteFatalFailure(UnityWebRequest webRequest)
        {
            Debug.LogError(string.Format(
                "GraphQL response error!\n{0}\n{1}\n{2}\n\nFalling back to locally saved content...",
                webRequest?.url,
                webRequest?.error,
                webRequest?.downloadHandler?.text
            ));

            // TODO UI display of error handling and option to try again

            // Fall back to loading content locally
            yield return LoadLocalContent();
        }

        /// <summary>
        /// Callback for graphql request error
        /// </summary>
        /// <param name="url"></param>
        /// <param name="error"></param>
        /// <param name="responseText"></param>
        /// <returns></returns>
        protected virtual IEnumerator OnRemoteFatalFailure(
            string url = null,
            string error = null,
            string responseText = null)
        {
            Debug.LogError(string.Format(
                "GraphQL response error!\n{0}\n{1}\n{2}\n\nFalling back to locally saved content...",
                url,
                error,
                responseText
            ));

            // Fall back to loading content locally
            yield return LoadLocalContent();
        }
        #endregion
    }

}
