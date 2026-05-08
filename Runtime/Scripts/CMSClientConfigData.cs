namespace rlmg.Tools.ContentLoading
{
    using System;

    [Serializable]
    public enum RequestModeOption
    {
        NoOverride = -1,
        GraphQL = 0,
        REST_GET = 1,
    }

    [Serializable]
    public enum ContentLocationRootOption
    {
        NoOverride = -1,
        StreamingAssets = 0,
        Desktop = 1,
        Application = 2
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class CMSClientConfigData
    {
        /// <summary>
        /// Whether or not to use the values in this config to load content.
        /// If false, default values set in the Unity Editor will be used.
        /// </summary>
        public bool useThisConfig = false;

        /// <summary>
        /// Whether or not to make a request to the server to load content.
        /// The base GraphQLContentLoader will fall back to local loading if false.
        /// </summary>
        public bool useServer = false;

        /// <summary>
        /// Whether or not to cache the content loaded from the server to
        /// the file contentFileName
        /// </summary>
        public bool doCache = true;

        /// <summary>
        /// The mode to use when making requests to the server.
        /// 
        /// Enum values, with the following options:
        /// -1 - NoOverride: Use the request mode set in the Unity Editor.
        /// 0 - GraphQL: Use GraphQL requests to the server.
        /// 1 - REST_GET: Use REST GET requests to the server.
        /// 2 - REST_POST: Use REST POST requests to the server.
        /// </summary>
        public RequestModeOption requestMode = RequestModeOption.NoOverride;

        /// <summary>
        /// The root directory to load content from.
        /// The main content directory is in this directory.
        /// 
        /// Enum value, with the following options:
        /// -1 - NoOverride: Use the content location root set in the Unity Editor.
        /// 0 - StreamingAssets: Load content from the StreamingAssets folder.
        /// 1 - Desktop: Load content from the Desktop folder.
        /// 2 - Application: Load content from the Application.dataPath folder.
        /// </summary>
        public ContentLocationRootOption contentLocationRoot = ContentLocationRootOption.NoOverride;

        /// <summary>
        /// The name of the directory at the content loading location to load content from.
        /// </summary>
        public string contentDirName = null;

        /// <summary>
        /// The name of the file to which content will be cached,
        /// and from which it will be loaded locally as a fallback.
        /// </summary>
        public string contentFileName = null;

        /// <summary>
        /// URL of the server to which to make the request to load content.
        /// </summary>
        public string serverURL = null;

        /// <summary>
        /// Endpoint for GraphQL requests to the server. This will be appended to the serverURL to make the full URL for the request.
        /// </summary>
        public string graphEndpoint = null;

        /// <summary>
        /// Endpoint for REST requests to the server, for assets. This will be appended to the serverURL to make the full URL for the request.
        /// Only useful when the server shares the serverURL for GraphQL and REST requests for assets, like Directus does.
        /// </summary>
        public string assetsEndpoint = null;

        /// <summary>
        /// Endpoint for REST requests to the server, for general purposes. This will be appended to the serverURL to make the full URL for the request.
        /// </summary>
        public string restEndpoint = null;

        /// <summary>
        /// Bearer token for authorization to make requests to the server. This will be added to the request headers when making requests to the server.
        /// </summary>
        public string authToken = null;

        /// <summary>
        /// GraphQL operation name to use in the request to the server. This will be added to the request body when making requests to the server.
        /// </summary>
        public string operationName = null;

        /// <summary>
        /// Filename of the GraphQL query to use in the request to the server. This will be read and added to the request body when making requests to the server.
        /// </summary>
        public string queryFileName = null;
    }

}