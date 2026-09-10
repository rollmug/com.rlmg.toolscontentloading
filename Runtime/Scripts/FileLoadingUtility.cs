namespace rlmg.Tools.ContentLoading
{
    using UnityEngine;
    using UnityEngine.Networking;
    using System;
    using System.Collections;
    using System.IO;
    using Newtonsoft.Json;

    public static class FileLoadingUtility
    {
        public static readonly char[] PathSplitCharacters = new char[] { '/', '\\' };

        public static string RemoveStartingPathSplitCharacter(string path)
        {
            foreach (char c in PathSplitCharacters)
            {
                if (path.StartsWith(c))
                    return path.Substring(1);
            }

            return path;
        }

        /// <summary>
        /// If remote, return input. Else, assume local, get a cross-platform filepath.
        /// </summary>
        /// <param name="rawPath"></param>
        /// <returns></returns>
        public static string GetProperUri(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                return rawPath;

            if (Uri.TryCreate(rawPath, UriKind.Absolute, out Uri uriResult))
            {
                return rawPath;
            }

            // assume it's a local file path
            return GetProperLocalUri(rawPath);
        }

        /// <summary>
        /// Get a cross-platform local filepath
        /// </summary>
        /// <param name="rawPath"></param>
        /// <returns></returns>
        private static string GetProperLocalUri(string rawPath)
        {
            if (rawPath.StartsWith("http"))
            {
                Debug.LogError("Path begins with http. Cannot convert to local file path.");
                return rawPath;
            }

            // Replace all backslashes with forward slashes
            string p = rawPath.Replace('\\', '/');

            // Create a URI to handle the file protocol (file://) and URL encoding
            UriBuilder uriBuilder = new UriBuilder("file", "", 0, p);

            // Convert to absolute URI (e.g., file:///C:/... or file:///Users/...)
            return uriBuilder.Uri.AbsoluteUri;
        }

        /// <summary>
        /// Writes a fallback TextAsset's text to path on disk, so a valid file exists there next time -
        /// intended to be called whenever the fallback is used because the on-disk file was missing,
        /// unreadable, or failed to parse. No-ops if asset is null.
        /// </summary>
        public static void WriteTextAssetToDisk(TextAsset asset, string path)
        {
            if (asset == null)
                return;

            try
            {
                File.WriteAllText(path, asset.text);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to write fallback text asset to disk at " + path + ": " + ex.Message);
            }
        }

        /// <summary>
        /// Loads text from path via UnityWebRequest (yielding across frames rather than blocking). If
        /// that fails and fallbackAsset is not null, uses the fallback's text instead and writes it to
        /// path (via <see cref="WriteTextAssetToDisk"/>) so a valid file exists there next time. If the
        /// load fails and there is no fallback, invokes onFailure with a description of the error instead
        /// of onLoaded.
        /// </summary>
        public static IEnumerator LoadTextWithFallbackCoroutine(
            string path,
            TextAsset fallbackAsset,
            string description,
            Action<string> onLoaded,
            Action<string> onFailure)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(GetProperUri(path)))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    onLoaded?.Invoke(webRequest.downloadHandler.text);
                    yield break;
                }

                if (fallbackAsset != null)
                {
                    Debug.LogWarning(string.Format(
                        "Failed to load {0} at {1}: {2}\nFalling back to assigned fallback text asset.",
                        description, path, webRequest.error));

                    WriteTextAssetToDisk(fallbackAsset, path);
                    onLoaded?.Invoke(fallbackAsset.text);
                    yield break;
                }

                onFailure?.Invoke(string.Format("Failed to load {0} at {1}: {2}", description, path, webRequest.error));
            }
        }

        /// <summary>
        /// Loads and JSON-deserializes a file via <see cref="LoadTextWithFallbackCoroutine"/>. If the
        /// on-disk file loads but fails to parse, and a fallback wasn't already used for the load itself,
        /// falls back to parsing fallbackAsset's text (also writing it to path). Invokes onFailure (with
        /// no call to onParsed) if loading and/or parsing fails without a usable fallback.
        /// </summary>
        public static IEnumerator LoadJsonWithFallbackCoroutine<T>(
            string path,
            TextAsset fallbackAsset,
            string description,
            Action<T> onParsed,
            Action<string> onFailure) where T : class
        {
            string text = null;
            bool usedFallback = false;
            bool loadFailed = false;

            yield return LoadTextWithFallbackCoroutine(
                path,
                fallbackAsset,
                description,
                loaded =>
                {
                    text = loaded;
                    usedFallback = fallbackAsset != null && loaded == fallbackAsset.text;
                },
                message =>
                {
                    loadFailed = true;
                    onFailure?.Invoke(message);
                });

            if (loadFailed)
                yield break;

            T parsed;
            try
            {
                parsed = JsonConvert.DeserializeObject<T>(text);
            }
            catch (JsonException ex)
            {
                if (usedFallback || fallbackAsset == null)
                {
                    onFailure?.Invoke(string.Format("Failed to parse {0}: {1}", description, ex.Message));
                    yield break;
                }

                Debug.LogWarning(string.Format(
                    "Failed to parse {0}: {1}\nFalling back to assigned fallback text asset.",
                    description, ex.Message));
                WriteTextAssetToDisk(fallbackAsset, path);

                try
                {
                    parsed = JsonConvert.DeserializeObject<T>(fallbackAsset.text);
                }
                catch (JsonException ex2)
                {
                    onFailure?.Invoke(string.Format("Failed to parse fallback text asset for {0}: {1}", description, ex2.Message));
                    yield break;
                }
            }

            onParsed?.Invoke(parsed);
        }

    }

}