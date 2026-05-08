namespace rlmg.Tools.ContentLoading
{
    using System;
    using System.Collections;
    using System.IO;
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
        /// The file path to this loader's config file, which contains serverURL, graphEndpoint, assetsEndpoint, authToken, operationName, etc.
        /// </summary>
        protected string localLoaderConfigFilePath
        {
            get
            {
                return Path.Combine(LocalContentDirectory, loaderConfigFileName);
            }
        }

        [Header("GraphQL Settings - Configurable by Config File")]
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
        /// The file path to the query text doc
        /// </summary>
        protected string localQueryFilePath
        {
            get
            {
                return Path.Combine(LocalContentDirectory, queryFileName);
            }
        }

        /// <summary>
        /// Optional. Used if doLoadQueryFromDisk is false.
        /// </summary>
        [Multiline]
        [SerializeField] protected string queryText;

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
        protected string graphURL
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
        public string AssetsURL
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
        protected string restURL
        {
            get
            {
                if (restURLOrEndpoint.StartsWith("http"))
                    return restURLOrEndpoint;

                return serverURL + "/" + restURLOrEndpoint;
            }
        }

        /// <summary>
        /// Any authentication token needed for the query.
        /// </summary>
        [SerializeField] protected string authToken;

        /// <summary>
        /// The operation name to post with the query.
        /// </summary>
        [SerializeField] protected string operationName;

        /// <summary>
        /// Manager class for caching.
        /// </summary>
        protected ContentCacher cacher;

        /// <summary>
        /// If either the config loader or query loader failed.
        /// </summary>
        public UnityEngine.Events.UnityEvent<string> AnySupportLoadFailed;


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
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator LoadLoaderConfig()
        {
            if (!doLoadLoaderConfigFromDisk)
                yield break;

            using (UnityWebRequest configRequest = UnityWebRequest.Get(localLoaderConfigFilePath))
            {
                yield return configRequest.SendWebRequest();

                if (configRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(string.Format("Loading local loader config file error: {0}\n{1}", configRequest.error, configRequest.downloadHandler.data));
                    
                    AnySupportLoadFailed?.Invoke(string.Format("Loading local loader config file error: {0}\n{1}", configRequest.error, configRequest.downloadHandler.data));
                    yield break; // if we can't load the loader config, we can't continue with loading the graph content
                }

                Debug.Log(string.Format(
                    "Successfully loaded {0} from disk. Attempting to use file contents to set loader settings for GraphQL endpoint {2} with file contents:\n{1}",
                    loaderConfigFileName, configRequest.downloadHandler.text, graphURL
                    ));

                CMSClientConfigData configData = JsonUtility.FromJson<CMSClientConfigData>(configRequest.downloadHandler.text);
                ApplyConfigSettings(configData);
            }
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
            cacher.DoCache = configData.doCache;

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

            if (configData.authToken != null)
                authToken = configData.authToken;

            if (configData.operationName != null)
                operationName = configData.operationName;

            if (configData.queryFileName != null)
                queryFileName = configData.queryFileName;
        }

        /// <summary>
        /// Load query file from disk.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator LoadQueryFromDisk()
        {
            if (!doLoadQueryFromDisk)
                yield break;

            using (UnityWebRequest queryRequest = UnityWebRequest.Get(localQueryFilePath))
            {
                yield return queryRequest.SendWebRequest();

                if (queryRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(string.Format(
                        "Loading local query file error: {0}\n{1}\n{2}",
                        localQueryFilePath,
                        queryRequest.error, queryRequest.downloadHandler.data)
                        );

                    AnySupportLoadFailed?.Invoke(string.Format("Loading local query file error: {0}\n{1}\n{2}", localQueryFilePath, queryRequest.error, queryRequest.downloadHandler.data));
                    yield break;
                }

                Debug.Log(string.Format(
                    "Successfully loaded {0} from disk. Attempting to post to GraphQL endpoint {2} with file contents:\n{1}",
                    queryFileName, queryRequest.downloadHandler.text, graphURL
                ));

                queryText = queryRequest.downloadHandler.text;
            }
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
                yield return OnRemoteFatalFailure(null);
                AnyLoadFailed?.Invoke(null);
                yield break;
            }

            //just showing that variables might be passed in this way
            //object variables = new { erasByIdId = erasByIdIdValue };
            object variables = new { };

            GraphPostData postData = new GraphPostData(queryText, variables);
            string json = JsonUtility.ToJson(postData);

            using (UnityWebRequest webRequest = UnityWebRequest.Post(graphURL, json, "application/json"))
            {
                if (!string.IsNullOrEmpty(authToken))
                    webRequest.SetRequestHeader("Authorization", "Bearer " + authToken);

                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    DidLoadSucceed = false;
                    yield return OnRemoteFatalFailure(webRequest);
                    AnyLoadFailed?.Invoke(webRequest);
                }
                else
                {
                    DidLoadSucceed = true;
                    yield return OnRemoteResponseSuccess(webRequest);
                    yield return AfterAnySuccess(webRequest);
                    AnyLoadSucceeded?.Invoke(webRequest);
                }
            }
        }

        protected virtual IEnumerator DoRestGetRequest()
        {
            if (requestMode != RequestMode.REST_GET)
                yield break;

            using (UnityWebRequest webRequest = UnityWebRequest.Get(restURL))
            {
                if (!string.IsNullOrEmpty(authToken))
                    webRequest.SetRequestHeader("Authorization", "Bearer " + authToken);

                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    DidLoadSucceed = false;
                    yield return OnRemoteFatalFailure(webRequest);
                    AnyLoadFailed?.Invoke(webRequest);
                }
                else
                {
                    DidLoadSucceed = true;
                    yield return OnRemoteResponseSuccess(webRequest);
                    yield return AfterAnySuccess(webRequest);
                    AnyLoadSucceeded?.Invoke(webRequest);
                }
            }
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

            if (cacher.DoCache)
                // Only save on remote CMS success, as opposed to saving even when loading locally
                cacher.CacheContent(webRequest.downloadHandler.text, localContentPath);
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
        #endregion
    }

}
