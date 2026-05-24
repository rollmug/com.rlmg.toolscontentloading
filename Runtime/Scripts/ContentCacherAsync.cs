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
        /// <summary>
        /// Optional amount of time to sleep between Tasks.
        /// A value of 0 will NOT yield execution in CacheTextAsync and CacheFileAsync.
        /// </summary>
        public int DebugSleepDur = 0;

        [Header("Async Settings")]
        [SerializeField] private int maxConcurrentCaches = 8;

        /// <summary>
        /// Guard that throttles number of concurrent Tasks
        /// </summary>
        private SemaphoreSlim cacheSemaphore;

        /// <summary>
        /// Used to cancel in-progress writes
        /// </summary>
        private CancellationTokenSource cacheCancellation;

        // Public state for other UI if needed
        public bool IsCaching { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            maxConcurrentCaches = Mathf.Min(1, maxConcurrentCaches);
            cacheSemaphore = new SemaphoreSlim(maxConcurrentCaches);
        }

        private void OnDestroy()
        {
            CancelCaching();

            cacheCancellation?.Dispose();
        }

        /// <summary>
        /// Key override - replace original functionality with async loading
        /// </summary>
        /// <param name="content"></param>
        /// <param name="path"></param>
        public override void CacheText(string content, string path)
        {
            if (!DoCacheText) return;

            ResetCancellation();

            // Fire-and-forget
            _ = CacheTextAsync(
                content,
                path,
                cacheCancellation.Token);
        }

        public override void CacheFile(byte[] bytes, string dstPath)
        {
            if (!DoCacheMedia) return;

            // Reset only at start of new series of file caches
            if (cacheCancellation == null || cacheCancellation.IsCancellationRequested)
                ResetCancellation();

            // Fire-and-forget
            _ = CacheFileAsync(
                dstPath,
                bytes,
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
        public override void CancelCaching()
        {
            base.CancelCaching();

            if (cacheCancellation == null)
                return;

            cacheCancellation.Cancel();

            if (doDebugLog)
                Debug.Log("Cancelling cache write...");
        }

        /// <summary>
        /// Set the maximum number of Tasks that may run concurrently
        /// </summary>
        /// <param name="newMax"></param>
        public void SetMaxConcurrentCaches(int newMax)
        {
            CancelCaching();

            maxConcurrentCaches = Mathf.Min(1, newMax);

            cacheSemaphore?.Dispose();

            cacheSemaphore = new SemaphoreSlim(maxConcurrentCaches);
        }

        /// <summary>
        /// Non-blocking cache write using Unity Awaitable
        /// </summary>
        public virtual async Awaitable CacheTextAsync(
            string content,
            string path,
            CancellationToken cancellationToken)
        {
            if (!DoCacheText)
                return;

            await cacheSemaphore.WaitAsync(cancellationToken);

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

                cacheSemaphore.Release();

                IsCaching = false;
            }
        }

        /// <summary>
        /// Non-blocking cache write using Unity Awaitable
        /// </summary>
        public virtual async Awaitable CacheFileAsync(
            string path,
            byte[] bytes,
            CancellationToken cancellationToken)
        {
            if (!DoCacheMedia)
                return;

            await cacheSemaphore.WaitAsync(cancellationToken);

            IsCaching = true;

            try
            {
                // Optional debug value to observe async
                // Must be done before moving to background thread
                if (DebugSleepDur > 0)
                    await Awaitable.WaitForSecondsAsync(DebugSleepDur);

                // Move file IO off main thread
                await Awaitable.BackgroundThreadAsync();

                if (DoSkipExistingFiles && File.Exists(path))
                    return;

                await File.WriteAllBytesAsync(
                    path,
                    bytes,
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

                cacheSemaphore.Release();

                IsCaching = false;
            }
        }

        
    }

}