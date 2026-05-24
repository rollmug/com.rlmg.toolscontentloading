namespace rlmg.Tools.ContentLoading
{
    using System.Collections;
    using System.IO;
    using UnityEngine;

    [RequireComponent(typeof(ContentLoader))]
    public class ContentCacher : MonoBehaviour
    {
        /// <summary>
        /// Source of truth for external data directory, etc.
        /// </summary>
        ContentLoader contentLoader;

        /// <summary>
        /// Do save text content to disk?
        /// </summary>
        [Header("Text Settings")]
        public bool DoCacheText;

        /// <summary>
        /// Do save media to disk?
        /// </summary>
        [Header("Media Settings")]
        public bool DoCacheMedia;

        /// <summary>
        /// Name of media directory where media may be saved to.
        /// </summary>
        [SerializeField]
        protected string localMediaCacheDirectoryName = "mediaCache";

        /// <summary>
        /// Path to directory where media may be saved to.
        /// </summary>
        protected string localMediaCacheDirectoryPath
        {
            get
            {
                if (contentLoader == null)
                    return null;

                string directoryName = string.IsNullOrEmpty(localMediaCacheDirectoryName) ? "mediaCache" : localMediaCacheDirectoryName;

                string path = Path.Combine(contentLoader.LocalContentDirectory, directoryName);

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
        }

        /// <summary>
        /// Do download media files if they are already exist?
        /// </summary>
        public bool DoSkipExistingFiles = false;

        [Header("Debug")]
        [SerializeField] protected bool doDebugLog = false;

        protected virtual void Awake()
        {
            contentLoader = GetComponent<ContentLoader>();
        }

        #region Text
        /// <summary>
        /// Write text to disk at LocalContentPath
        /// </summary>
        /// <param name="text">json</param>
        public virtual void CacheText(string content, string path)
        {
            if (!DoCacheText) return;

            File.WriteAllText(path, PrettifyJson(content));

            if (doDebugLog)
                Debug.Log("Cached content to disk at " + path);
        }

        /// <summary>
        /// Prettify method with no external libraries.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string PrettifyJson(string json)
        {
            var sb = new System.Text.StringBuilder();
            bool quoted = false;
            int indent = 0;

            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];

                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);

                        if (!quoted)
                        {
                            sb.AppendLine();
                            indent++;
                            sb.Append(new string(' ', indent * 4));
                        }
                        break;

                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            indent--;
                            sb.Append(new string(' ', indent * 4));
                        }

                        sb.Append(ch);
                        break;

                    case '"':
                        sb.Append(ch);

                        bool escaped = false;
                        int index = i;

                        while (index > 0 && json[--index] == '\\')
                            escaped = !escaped;

                        if (!escaped)
                            quoted = !quoted;

                        break;

                    case ',':
                        sb.Append(ch);

                        if (!quoted)
                        {
                            sb.AppendLine();
                            sb.Append(new string(' ', indent * 4));
                        }
                        break;

                    case ':':
                        sb.Append(ch);

                        if (!quoted)
                            sb.Append(" ");

                        break;

                    default:
                        if (!quoted && char.IsWhiteSpace(ch))
                            continue;

                        sb.Append(ch);
                        break;
                }
            }

            return sb.ToString();
        }
        #endregion

        #region Media
        /// <summary>
        /// Format path for a given media file in the LocalMediaCacheDirectory
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>Path to file</returns>
        public string GetLocalMediaPath(string filename)
        {
            string path = localMediaCacheDirectoryPath;

            if (path == null)
                return null;

            return Path.Combine(path, filename);
        }

        /// <summary>
        /// Format path and create subdirectories for given media file in the LocalMediaCacheDirectory
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="subDirectoryPathFragment">Path fragment of subdirectories of LocalMediaCacheDirectory</param>
        /// <returns></returns>
        public string GetLocalMediaPath(string filename, string subDirectoryPathFragment)
        {
            string subDirectoryPathFull = Path.Combine(localMediaCacheDirectoryPath, subDirectoryPathFragment);

            if (!Directory.Exists(subDirectoryPathFull))
                Directory.CreateDirectory(subDirectoryPathFull);

            return Path.Combine(subDirectoryPathFull, filename);
        }

        /// <summary>
        /// Download the given file and save to disk
        /// </summary>
        /// <param name="srcPath">URL to download from</param>
        /// <param name="dstPath">Local path to save to</param>
        /// <returns></returns>
        protected virtual IEnumerator CacheFileRoutine(string srcPath, string dstPath)
        {
            // Skip for existing files if we don't want to overwrite them
            if (DoSkipExistingFiles && File.Exists(dstPath))
                yield break;

            yield return MediaLoadingUtility.LoadFileCoroutine(
                srcPath,
                (bytes) =>
                {
                    File.WriteAllBytes(dstPath, bytes);
                    SaveMediaToDiskSuccess();
                },
                SaveMediaToDiskFailure);
        }

        /// <summary>
        /// Stop any ongoing concurrent caching.
        /// In the base method, this is only Coroutines.
        /// </summary>
        public virtual void CancelCaching()
        {
            StopAllCoroutines();
        }

        public virtual void CacheFile(string srcPath, string dstPath)
        {
            StartCoroutine(
                CacheFileRoutine(srcPath, dstPath));
        }

        public virtual void CacheFile(byte[] bytes, string dstPath)
        {
            // Skip for existing files if we don't want to overwrite them
            if (DoSkipExistingFiles && File.Exists(dstPath))
                return;

            File.WriteAllBytes(dstPath, bytes);
            SaveMediaToDiskSuccess();
        }

        /// <summary>
        /// Callback after successfully downloading and saving media to disk
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected virtual void SaveMediaToDiskSuccess()
        {
        }

        /// <summary>
        /// Callback after failed media download
        /// </summary>
        /// <param name="request">The failed request</param>
        /// <returns></returns>
        protected virtual void SaveMediaToDiskFailure(string message, string uri)
        {
            if (doDebugLog)
                Debug.LogError("Save Media to Disk Failure: " + message+ "\nat " + uri);
        }
        #endregion

    }
}