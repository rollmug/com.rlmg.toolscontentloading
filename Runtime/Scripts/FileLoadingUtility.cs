namespace rlmg.Tools.ContentLoading
{
    using UnityEngine;
    using System;

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
        /// If local, get a cross-platform filepath. Else, return input.
        /// </summary>
        /// <param name="rawPath"></param>
        /// <returns></returns>
        public static string GetProperUri(string rawPath)
        {
            if (rawPath.StartsWith("http"))
            {
                return rawPath;
            }

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

    }

}