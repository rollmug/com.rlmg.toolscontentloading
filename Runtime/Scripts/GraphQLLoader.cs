namespace rlmg.Tools.ContentLoading
{
    using System.Collections;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Networking;
    public abstract class GraphQLLoader : ContentLoader
    {
        protected bool doLoadQueryFromDisk = true; // todo - use

        /// <summary>
        /// File name of GraphQL-formatted query text doc
        /// Assumed to be located in localContentDirectory
        /// </summary>
        [Header("GraphQL Settings")]
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
        /// Any authentication token needed for the query.
        /// </summary>
        [SerializeField] protected string authToken;

        /// <summary>
        /// The operation name to post with the query.
        /// </summary>
        [SerializeField] protected string operationName;


        #region Graphql Loading
        /// <summary>
        /// Override the default load method with graphql-specific logic
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator MainLoadContent()
        {
            Debug.Log("Attempting to load content from GraphQL endpoint...");
            yield return StartCoroutine(LoadGraphContent());
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

        /// <summary>
        /// First loads the query text file from disk, then
        /// Posts the graphql query and handles response
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator LoadGraphContent()
        {
            // todo - optional
            using (UnityWebRequest queryRequest = UnityWebRequest.Get(localQueryFilePath))
            {
                yield return queryRequest.SendWebRequest();

                if (queryRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(string.Format("Loading local query file error: {0}\n{1}", queryRequest.error, queryRequest.downloadHandler.data));

                    DidLoadSucceed = false;
                    yield return OnGraphLoadFailure(queryRequest);
                    AnyLoadFailed?.Invoke(queryRequest);
                }
                else
                {
                    Debug.Log(string.Format(
                        "Successfully loaded {0} from disk. Attempting to post to GraphQL endpoint {2} with file contents:\n{1}",
                        queryFileName, queryRequest.downloadHandler.text, graphURL
                        ));

                    //just showing that variables might be passed with way
                    //object variables = new { erasByIdId = erasByIdIdVariable };
                    object variables = new { };

                    GraphPostData postData = new GraphPostData(queryRequest.downloadHandler.text, variables);
                    string json = JsonUtility.ToJson(postData);

                    using (UnityWebRequest webRequest = UnityWebRequest.Post(graphURL, json, "application/json"))
                    {
                        if (!string.IsNullOrEmpty(authToken))
                            webRequest.SetRequestHeader("Authorization", "Bearer " + authToken);

                        yield return webRequest.SendWebRequest();

                        if (webRequest.result != UnityWebRequest.Result.Success)
                        {
                            DidLoadSucceed = false;
                            yield return OnGraphLoadFailure(webRequest);
                            AnyLoadFailed?.Invoke(webRequest);
                        }
                        else
                        {
                            DidLoadSucceed = true;
                            yield return OnGraphResponseSuccess(webRequest);
                            yield return AfterAnySuccess(webRequest);
                            AnyLoadSucceeded?.Invoke(webRequest);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Callback for graphql request success
        /// </summary>
        /// <param name="webRequest">The successful UnityWebRequest</param>
        /// <returns></returns>
        protected virtual IEnumerator OnGraphResponseSuccess(UnityWebRequest webRequest)
        {
            if (string.IsNullOrEmpty(webRequest?.downloadHandler?.text)) { yield break; }

            // Only save on remote CMS success, as opposed to saving even when loading locally
            SaveContentDataToDisk(PrettifyJson(webRequest.downloadHandler.text));
        }

        /// <summary>
        /// Callback for graphql request error
        /// </summary>
        /// <param name="webRequest">The failed request</param>
        /// <returns></returns>
        protected virtual IEnumerator OnGraphLoadFailure(UnityWebRequest webRequest)
        {
            Debug.LogError(string.Format(
                "GraphQL response error!\n{0}\n{1}\n{2}\n\nFalling back to locally saved content...",
                webRequest.url,
                webRequest.error,
                webRequest.downloadHandler.text
            ));

            // TODO UI display of error handling and option to try again

            // Fall back to loading content locally
            yield return LoadLocalContent();
        }
        #endregion
    }

}
