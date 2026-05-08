namespace rlmg.Tools.ContentLoading
{
    using System;
    using System.IO;
    using System.Threading;
    using UnityEngine;

    /// <summary>
    /// Helper component for saving contents to disk asynchronously,
    /// using Unity's Awaitable class.
    /// </summary>
    /// <remarks>Includes methods for managing the Awaitable.</remarks>
    public class ContentCacherAsync : ContentCacher
    {
        public int DebugSleepDur = 0;

        // Used to cancel in-progress writes
        private CancellationTokenSource cacheCancellation;

        // Public state for other UI if needed
        public bool IsCaching { get; private set; }

        /// <summary>
        /// Key override - replace original functionality with async loading
        /// </summary>
        /// <param name="content"></param>
        /// <param name="path"></param>
        public override void CacheContent(string content, string path)
        {
            if (!DoCache) return;

            ResetCancellation();

            // Fire-and-forget
            _ = CacheContentAsync(
                content,
                path,
                cacheCancellation.Token);
        }

        /// <summary>
        /// Reset cancellation token for a new write
        /// </summary>
        private void ResetCancellation()
        {
            cacheCancellation?.Cancel();
            cacheCancellation?.Dispose();

            cacheCancellation = new CancellationTokenSource();
        }

        /// <summary>
        /// Cancel active write
        /// </summary>
        public void CancelCacheWrite()
        {
            if (cacheCancellation == null)
                return;

            cacheCancellation.Cancel();

            if (doDebugLog)
                Debug.Log("Cancelling cache write...");
        }

        /// <summary>
        /// Non-blocking cache write using Unity Awaitable
        /// </summary>
        public virtual async Awaitable CacheContentAsync(
            string content,
            string path,
            CancellationToken cancellationToken)
        {
            if (!DoCache)
                return;

            IsCaching = true;

            try
            {
                // Optional debug value to observe async
                // Must be done before moving to background thread
                if (DebugSleepDur > 0)
                    await Awaitable.WaitForSecondsAsync(DebugSleepDur);

                // Move file IO off main thread
                await Awaitable.BackgroundThreadAsync();

                await File.WriteAllTextAsync(
                    path,
                    PrettifyJson(content),
                    cancellationToken);

                // Return to Unity main thread
                await Awaitable.MainThreadAsync();

                if (doDebugLog)
                    Debug.Log("Cached content to disk at " + path);
            }
            catch (OperationCanceledException)
            {
                await Awaitable.MainThreadAsync();

                if (doDebugLog)
                    Debug.Log("Cache write cancelled");
            }
            catch (Exception ex)
            {
                await Awaitable.MainThreadAsync();

                Debug.LogError("Failed to cache content: " + ex.Message);
            }
            finally
            {
                await Awaitable.MainThreadAsync();

                IsCaching = false;
            }
        }

        private void OnDestroy()
        {
            CancelCacheWrite();

            cacheCancellation?.Dispose();
        }
    }

}