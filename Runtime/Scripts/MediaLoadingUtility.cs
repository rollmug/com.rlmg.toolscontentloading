namespace rlmg.Tools.ContentLoading
{
    using System;
    using System.Collections;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Networking;
    public static class MediaLoadingUtility
    {
        public static string[] imageExtensions = {
            ".PNG", ".JPG", ".JPEG", ".BMP", ".GIF", ".TIFF", ".TIF"
        };

        public static string[] videoExtensions =
        {
            ".MP4", ".MOV"
        };

        public enum FileType
        {
            Unknown = 0,
            Image = 1,
            Video = 2,
            Audio = 3
        }

        public static bool IsImageFile(string path)
        {
            return -1 != Array.IndexOf(imageExtensions, Path.GetExtension(path).ToUpperInvariant());
        }

        public static bool IsVideoFile(string path)
        {
            return -1 != Array.IndexOf(videoExtensions, Path.GetExtension(path).ToUpperInvariant());
        }

        public static FileType GetFileType(string path)
        {
            if (IsImageFile(path))
                return FileType.Image;
            else if (IsVideoFile(path))
                return FileType.Video;
            else
                return FileType.Unknown;
        }

        public static char[] pathSplitCharacters = new char[] { '/', '\\' };

        public static string RemoveStartingPathSplitCharacter(string path)
        {
            foreach (char c in pathSplitCharacters)
            {
                if (path.StartsWith(c))
                    return path.Substring(1);
            }
            return path;
        }

        /// <summary>
        /// Downloads or loads a texture from the given uri or path, and runs the 'setter' Action with the downloadHandler.texture as a parameter.
        /// </summary>
        /// <param name="uri">Path or web address from which to load the texture.</param>
        /// <param name="successCallback">Intended use is for this method to set the value of its parameter as the downloadHandler.texture.</param>
        /// <returns></returns>
        public static IEnumerator LoadTexture2DFromPath(string uri, Action<Texture2D> successCallback, Action errorCallback = null)
        {
            if (!IsImageFile(uri))
            {
                Debug.LogError(String.Format("File {0} is not a supported image type.", uri));
                errorCallback?.Invoke();
                yield break;
            }

            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(uri))
            {
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(String.Format("Error: {0} at {1}", webRequest.error, uri));
                        errorCallback?.Invoke();
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(String.Format("HTTP Error: {0} at {1}", webRequest.error, uri));
                        errorCallback?.Invoke();
                        break;
                    case UnityWebRequest.Result.Success:
                        Texture2D texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
                        if (texture == null)
                        {
                            Debug.LogError(String.Format("Null texture for {0}", uri));
                            errorCallback?.Invoke();
                            break;
                        }
                        successCallback?.Invoke(texture);
                        break;
                }
            }
        }
    }

}
