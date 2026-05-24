namespace rlmg.Tools.ContentLoading
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Networking;

    public static class MediaLoadingUtility
    {
        public enum FileType
        {
            Unknown = 0,
            Image = 1,
            Video = 2,
            Audio = 3
        }

        public static readonly string[] ImageExtensions = {
            ".PNG", ".JPG", ".JPEG", ".BMP", ".GIF", ".TIFF", ".TIF"
        };

        public static readonly string[] VideoExtensions =
        {
            ".MP4", ".MOV"
        };

        public static readonly char[] PathSplitCharacters = new char[] { '/', '\\' };

        /// <summary>
        /// Maximum number of simultaneous downloads.
        /// </summary>
        private static int maxConcurrentDownloads = 8;

        /// <summary>
        /// Read-only property for maximum number of simulataneous downloads
        /// </summary>
        public static int MaxConcurrentDownloads => maxConcurrentDownloads;

        /// <summary>
        /// Guard that throttles number of concurrent downloads
        /// </summary>
        private static SemaphoreSlim downloadSemaphore =
            new SemaphoreSlim(maxConcurrentDownloads);

        // =======================================================================
        // ASYNC MANAGEMENT
        // =======================================================================

        public static void SetMaxCurrentDownloads(int newMax)
        {
            if (downloadSemaphore != null)
                if (downloadSemaphore.CurrentCount != maxConcurrentDownloads)
                {
                    Debug.LogError("The download Semaphore is in use by a Task and should not be changed");
                    return;
                }

            downloadSemaphore?.Dispose();

            maxConcurrentDownloads = newMax;
            downloadSemaphore = new SemaphoreSlim(maxConcurrentDownloads);
        }

        // =======================================================================
        // LOAD SINGLE FILE
        // =======================================================================

        /// <summary>
        /// Downloads or loads a file from the given uri or path,
        /// and runs the 'setter' Action with the downloadHandler.texture as a parameter.
        /// </summary>
        /// <param name="uri">Path or web address from which to load the texture.</param>
        /// <param name="onSuccess">Action to take on success.</param>
        /// <param name="onError">Action to take on error.</param>
        /// <returns></returns>
        public static IEnumerator LoadFileCoroutine(
            string uri,
            Action<byte[]> onSuccess,
            Action<string, string> onError = null)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError(String.Format("Error: {0} at {1}", webRequest.error, uri));
                        onError?.Invoke(webRequest.error, uri);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError(String.Format("HTTP Error: {0} at {1}", webRequest.error, uri));
                        onError?.Invoke(webRequest.error, uri);
                        break;
                    case UnityWebRequest.Result.Success:
                        onSuccess?.Invoke(webRequest.downloadHandler.data);
                        break;
                }
            }
        }

        /// <summary>
        /// Downloads or loads a file from the given uri or path
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<byte[]> LoadFileAsync(
            string uri,
            CancellationToken cancellationToken = default,
            Action<string, string> onError = null)
        {
            await downloadSemaphore.WaitAsync(cancellationToken);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using UnityWebRequest request = UnityWebRequest.Get(uri);

                using CancellationTokenRegistration registration =
                    cancellationToken.Register(() =>
                    {
                        request.Abort();
                    });

                UnityWebRequestAsyncOperation operation =
                    request.SendWebRequest();

                while (!operation.isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Yield();
                }

                cancellationToken.ThrowIfCancellationRequested();

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:

                        return request.downloadHandler.data;

                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.ProtocolError:
                    case UnityWebRequest.Result.DataProcessingError:

                        onError?.Invoke(
                            $"File load failed at {uri}: {request.error}", uri);

                        return null;

                    default:
                        return null;
                }
            }
            catch (OperationCanceledException e)
            {
                return null;
            }
            finally
            {
                downloadSemaphore.Release();
            }
        }

        // =======================================================================
        // LOAD SINGLE TEXTURE 
        // =======================================================================

        /// <summary>
        /// Downloads or loads a texture from the given uri or path,
        /// and runs the 'setter' Action with the downloadHandler.texture as a parameter.
        /// </summary>
        /// <param name="uri">Path or web address from which to load the texture.</param>
        /// <param name="textureSetter">Intended use is for this method to set the value of its parameter as the downloadHandler.texture.</param>
        /// <param name="onError">Action to take on error.</param>
        /// <returns></returns>
        public static IEnumerator LoadTextureCoroutine(
            string uri,
            Action<Texture2D> textureSetter,
            Action<string, string> onError = null,
            string fallbackUri = null)
        {
            if (!IsImageFile(uri))
            {
                onError?.Invoke("Not supported image type.", uri);
                yield break;
            }

            Texture2D texture = null;

            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(uri))
            {
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:

                        onError?.Invoke(webRequest.error, uri);

                        break;

                    case UnityWebRequest.Result.ProtocolError:

                        onError?.Invoke(webRequest.error, uri);

                        break;

                    case UnityWebRequest.Result.Success:

                        texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;

                        if (texture == null)
                        {
                            onError?.Invoke("Null texture.", uri);
                            break;
                        }

                        break;
                }
            }

            if (texture != null)
            {
                textureSetter?.Invoke(texture);

                yield break;
            }

            // Fall back
            if (!string.IsNullOrEmpty(fallbackUri))
                yield return LoadTextureCoroutine(
                    fallbackUri,
                    textureSetter,
                    onError);
            
        }

        /// <summary>
        /// Downloads or loads a texture from the given uri or path
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<Texture2D> LoadTextureAsync(
            string uri,
            CancellationToken cancellationToken = default,
            Action<string, string> onError = null,
            string fallbackUri = null)
        {
            Texture2D texture = null;

            await downloadSemaphore.WaitAsync(cancellationToken);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using UnityWebRequest request =
                    UnityWebRequestTexture.GetTexture(uri);

                using CancellationTokenRegistration registration =
                    cancellationToken.Register(() =>
                    {
                        request.Abort();
                    });

                UnityWebRequestAsyncOperation operation =
                    request.SendWebRequest();

                while (!operation.isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Yield();
                }

                cancellationToken.ThrowIfCancellationRequested();

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:

                        texture = DownloadHandlerTexture.GetContent(request);

                        if (texture == null)
                        {
                            onError?.Invoke("Loaded texture is null.", uri);
                        }

                        break;

                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.ProtocolError:
                    case UnityWebRequest.Result.DataProcessingError:

                        onError?.Invoke(
                            $"Texture load failed at {uri}: {request.error}", uri);

                        break;

                    default:
                        break;
                }
            }
            catch (OperationCanceledException e)
            {
                return null;
            }
            finally
            {
                downloadSemaphore.Release();
            }

            if (texture != null)
                return texture;

            // Fall back
            if (!string.IsNullOrEmpty(fallbackUri))
                return await LoadTextureAsync(
                    fallbackUri,
                    cancellationToken,
                    onError);

            return null;
        }

        // =======================================================================
        // LOAD TASK -> COROUTINE BRIDGE
        // =======================================================================

        public static IEnumerator WaitForTask(Task task)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                throw task.Exception;

            if (task.IsCanceled)
                yield break;
        }

        public static IEnumerator WaitForTask<T>(
            Task<T> task,
            Action<T> onComplete = null)
        {
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                throw task.Exception;

            if (task.IsCanceled)
                yield break;

            onComplete?.Invoke(task.Result);
        }

        // =======================================================================
        // FILE TYPES
        // =======================================================================

        public static bool IsImageFile(string path)
        {
            return -1 != Array.IndexOf(ImageExtensions, Path.GetExtension(path).ToUpperInvariant());
        }

        public static bool IsVideoFile(string path)
        {
            return -1 != Array.IndexOf(VideoExtensions, Path.GetExtension(path).ToUpperInvariant());
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

        // =======================================================================
        // PATHS
        // =======================================================================

        public static string RemoveStartingPathSplitCharacter(string path)
        {
            foreach (char c in PathSplitCharacters)
            {
                if (path.StartsWith(c))
                    return path.Substring(1);
            }
            return path;
        }

        
    }

}
