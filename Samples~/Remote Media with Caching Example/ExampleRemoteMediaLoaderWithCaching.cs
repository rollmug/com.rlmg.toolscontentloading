namespace rlmg.Tools.ContentLoading.Examples
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;
    using UnityEngine.Events;

    /// <summary>
    /// This example demonstrates the following:
    /// 1. Use of MediaLoadingUtility methods for loading images from a remote source;
    /// 2. Comparison of (a) sequential loading with Coroutines, and (b) concurrent loading with Tasks; and
    /// 3. Use of the ContentCacher class and ContentCacherAsync subclass.
    /// </summary>
    /// <remarks>
    /// Regarding (2), Coroutines might also be run concurrently, and Tasks might be run sequentially.
    /// This example uses them differently because the community traditionally uses Coroutines for
    /// sequential, blocking operations, and Tasks for concurrent, blocking operations.
    /// </remarks>
    /// <remarks>
    /// Regarding (3), in this example, ContentCacher and ContentCacherAsync may be used interchangeably;
    /// caching happens in a 'Fire-and-Forget' style.
    /// The only suggested management is to cancel ongoing caching before starting a new round of it (see below).
    /// </remarks>
    public class ExampleRemoteMediaLoaderWithCaching : GraphQLLoader
    {
        [Header("Example-Specific Settings")]
        /// <summary>
        /// Safe template for where to retrieve Pokémon images. The PokéAPI's sprite endpoint is complex, so this shortcuts getting the image URL endpoints using a known safe template.
        /// </summary>
        [Tooltip("Safe template for where to retrieve Pokémon images. The PokéAPI's sprite endpoint is complex, so this shortcuts getting the image URL endpoints using a known safe template.")]
        [SerializeField] string imageURL;

        /// <summary>
        /// Subfolder in media cache directory for this example's images
        /// </summary>
        [SerializeField] string imageCacheSubfolder = "pokemon-sprites";
        
        /// <summary>
        /// Made public so other MonoBehaviours can access the data after it's loaded, as needed.
        /// </summary>
        public PokemonSpecies[] Data;

        /// <summary>
        /// For manual media loading Task cancellation
        /// </summary>
        /// <remarks>Should be used whenever the loading coroutine is stopped.</remarks>
        private CancellationTokenSource mediaLoadingCancellation;

        /// <summary>
        /// Whether or not to use asynchronous, concurrent loading vs. synchronous, sequential loading
        /// </summary>
        [SerializeField] private bool doAsync = true;

        /// <summary>
        /// How many Tasks may be executed simultaneously?
        /// Only used in Awake (see below) to set the static Semaphore in MediaLoadingUtility
        /// </summary>
        [SerializeField]
        [Range(1, 32)]
        private int numConcurrentLoadsOnAwake = 8;

        public bool IsLoadingMedia { get; private set; }

        [SerializeField] Toggle toggleDoCache;
        [SerializeField] Toggle toggleDoAsync;
        [SerializeField] Toggle toggleDoSkipExisting;

        public UnityEvent<PokemonSpecies> ItemLoaded;

        protected override void Awake()
        {
            base.Awake();

            numConcurrentLoadsOnAwake = Mathf.Max(1, numConcurrentLoadsOnAwake);
            if (numConcurrentLoadsOnAwake != MediaLoadingUtility.MaxConcurrentDownloads)
                MediaLoadingUtility.SetMaxCurrentDownloads(numConcurrentLoadsOnAwake);

            toggleDoCache.isOn = cacher.DoCacheMedia;
            toggleDoAsync.isOn = doAsync;
            toggleDoSkipExisting.isOn = cacher.DoSkipExistingFiles;
        }

        /// <summary>
        /// Key override where we load images.
        /// The base method does nothing.
        /// </summary>
        /// <param name="webRequest"></param>
        /// <returns></returns>
        protected override IEnumerator AfterAnySuccess(UnityWebRequest webRequest)
        {
            Data = JsonUtility.FromJson<PokemonGraphDataWrapper>(webRequest.downloadHandler.text).data.pokemonspecies;

            if (UseServer)
                yield return LoadImages();
        }

        /// <summary>
        /// Helper method for cancelling the ongoing media loading Task
        /// and preparing for a new one
        /// </summary>
        /// <remarks>This should be called to clean up any ongoing Tasks before starting new ones.</remarks>
        private void ResetCancellation()
        {
            mediaLoadingCancellation?.Cancel();
            mediaLoadingCancellation?.Dispose();

            mediaLoadingCancellation = new CancellationTokenSource();
        }

        /// <summary>
        /// Stop asynchronous (Tasks and Coroutines) loading
        /// </summary>
        public void CancelLoading()
        {
            // Clean up any caching Coroutines or Tasks otherwise managed by cacher
            cacher.CancelCaching();

            // Clean up the media loading Coroutine managed by this MonoBehaviour
            StopAllCoroutines();

            // Clean up the media loading Tasks managed by this MonoBehaviour
            ResetCancellation();
        }

        /// <summary>
        /// Concurrently downloads images for each retrieved pokemon.
        /// If not caching, chooses small optimization for texture loading.
        /// 
        /// Because the PokéAPI's sprite endpoint is complex, this shortcuts getting the image URL endpoints
        /// using a known safe template.
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadImages()
        {
            if (Data == null)
                yield break;

            // Clean up any caching Coroutines or Tasks otherwise managed by cacher
            cacher.CancelCaching();

            // Clean up any media loading Tasks managed by this MonoBehaviour
            ResetCancellation();

            DateTime start = DateTime.Now;

            if (doAsync)
            {
                Task load = LoadImagesAsync(mediaLoadingCancellation.Token);
                yield return MediaLoadingUtility.WaitForTask(load);
            }
            else
            {
                yield return LoadImagesRoutine();
            }

            Debug.Log(
                string.Format(
                    "Loading images completed in {0} seconds",
                    (DateTime.Now - start).TotalSeconds
                    ));
        }

        #region Coroutine Loading
        /// <summary>
        /// Load an image for each data item
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadImagesRoutine()
        {
            IsLoadingMedia = true;

            for (int i = 0; i < Data.Length; i++)
            {
                PokemonSpecies item = Data[i];

                string srcUri = string.Format("{0}/{1}.png", imageURL, item.id);

                string cacheUri = cacher.GetLocalMediaPath(
                    string.Format("{0}.png", item.id),
                    imageCacheSubfolder);

                yield return LoadCacheAndCreateTextureRoutine(
                    item,
                    srcUri,
                    cacheUri);

                ItemLoaded?.Invoke(item);
            }

            IsLoadingMedia = false;
        }

        /// <summary>
        /// Load a texture from source,
        /// conditionally cache it, and
        /// set a field on item with a Texture2D object.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="srcUri"></param>
        /// <param name="cacheUri"></param>
        /// <param name="doCache"></param>
        /// <returns></returns>
        /// <remarks>
        /// Small optimization for texture loading using Unity's UnityWebRequestTexture (see MediaLoadingUtility)
        /// </remarks>
        private IEnumerator LoadCacheAndCreateTextureRoutine(
            PokemonSpecies item,
            string srcUri,
            string cacheUri)
        {
            bool willCache = false;

            if (cacher.DoCacheMedia)
            {
                if (cacher.DoSkipExistingFiles && File.Exists(cacheUri))
                    willCache = false;
                else
                    willCache = true;
            }

            if (willCache)
            {
                byte[] bytes = null;

                yield return MediaLoadingUtility.LoadFileCoroutine(
                    srcUri,
                    b => { bytes = b; },
                    OnMediaLoadingError);

                // check for a failed load
                if (bytes == null || bytes.Length == 0)
                {
                    // fallback to loading from anything already cached
                    yield return MediaLoadingUtility.LoadTextureCoroutine(
                        cacheUri,
                        texture => { item.Texture = texture; },
                        OnMediaLoadingError);

                    yield break; // exit early
                }

                // Fire-and-forget
                cacher.CacheFile(bytes, cacheUri);

                // Load into Texture2D object
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(bytes, markNonReadable: true);

                // Assign to item fields
                item.Texture = tex;
                item.TextureCachePath = cacheUri;
            }
            else
            {
                yield return MediaLoadingUtility.LoadTextureCoroutine(
                        srcUri,
                        texture => { item.Texture = texture; },
                        OnMediaLoadingError,
                        cacheUri);
            }
        }
        #endregion

        #region Async Loading
        /// <summary>
        /// Load an image for each data item
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task LoadImagesAsync(
            CancellationToken cancellationToken
            )
        {
            IsLoadingMedia = true;

            try
            {
                await Awaitable.MainThreadAsync();

                Task[] tasks = new Task[Data.Length];

                for (int i = 0; i < Data.Length; i++)
                {
                    PokemonSpecies item = Data[i];

                    string srcUri = string.Format("{0}/{1}.png", imageURL, item.id);

                    string cacheUri = cacher.GetLocalMediaPath(
                        string.Format("{0}.png", item.id),
                        imageCacheSubfolder);

                    tasks[i] = LoadCacheAndCreateTextureAsync(
                        item,
                        srcUri,
                        cacheUri,
                        cancellationToken
                        );
                }

                await Task.WhenAll( tasks );
            }
            catch (OperationCanceledException)
            {
                await Awaitable.MainThreadAsync();
            }
            catch (Exception ex)
            {
                await Awaitable.MainThreadAsync();
                Debug.LogError("Exception raised while loading media: " + ex.Message);
            }
            finally
            {
                await Awaitable.MainThreadAsync();
                IsLoadingMedia = false;
            }
        }

        /// <summary>
        /// Load a texture from source, conditionally cache it, and set a field on item with a Texture2D object.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="srcUri"></param>
        /// <param name="cacheUri"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="doCache"></param>
        /// <returns></returns>
        /// <remarks>Small optimization for texture loading using Unity's UnityWebRequestTexture (see MediaLoadingUtility)</remarks>
        private async Task LoadCacheAndCreateTextureAsync(
            PokemonSpecies item,
            string srcUri,
            string cacheUri,
            CancellationToken cancellationToken = default)
        {
            bool willCache = false;
            
            if (cacher.DoCacheMedia)
            {
                // Do I/O on Background Thread
                await Awaitable.BackgroundThreadAsync();

                if (cacher.DoSkipExistingFiles && File.Exists(cacheUri))
                    willCache = false;
                else
                    willCache = true;

                await Awaitable.MainThreadAsync();
            }

            if (willCache)
            {
                byte[] bytes = await MediaLoadingUtility.LoadFileAsync(
                    srcUri,
                    cancellationToken,
                    OnMediaLoadingError);

                // check for a failed load
                if (bytes == null || bytes.Length == 0)
                {
                    // fallback to loading from cache
                    Texture2D texture = await MediaLoadingUtility.LoadTextureAsync(
                        cacheUri,
                        cancellationToken,
                        OnMediaLoadingError);

                    if (texture  == null)
                    {
                        OnMediaLoadingError("Texture loaded from cache is null.", cacheUri);
                        return;
                    }

                    item.Texture = texture;
                    ItemLoaded?.Invoke(item);

                    return; // exit early
                }

                // Fire-and-forget
                cacher.CacheFile(bytes, cacheUri);

                // Load into Texture2D object
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(bytes, markNonReadable:true);

                // Assign to item fields
                item.Texture = tex;
                ItemLoaded?.Invoke(item);
            }
            else
            {
                Texture2D texture = await MediaLoadingUtility.LoadTextureAsync(
                    srcUri,
                    cancellationToken,
                    OnMediaLoadingError,
                    cacheUri);

                if (texture == null)
                {
                    OnMediaLoadingError("Texture loaded from cache is null.", srcUri);
                    return;
                }

                item.Texture = texture;
                ItemLoaded?.Invoke(item);

                return;
            }
        }
        #endregion

        /// <summary>
        /// Error callback for any failed media loads
        /// </summary>
        /// <param name="message"></param>
        /// <param name="uri"></param>
        private void OnMediaLoadingError(string message, string uri)
        {
            Debug.LogError(
                string.Format(
                    "Media Loading Error at {0}:\n{1}",
                    message,
                    uri)
                );
        }

        #region UI
        /// <summary>
        /// Callback for toggle
        /// </summary>
        /// <param name="value"></param>
        public void OnToggleDoCache(bool value)
        {
            cacher.DoCacheMedia = value;
        }

        /// <summary>
        /// Callback for toggle
        /// </summary>
        /// <param name="value"></param>
        public void OnToggleDoAsync(bool value)
        {
            doAsync = value;
        }

        /// <summary>
        /// Callback for toggle
        /// </summary>
        /// <param name="value"></param>
        public void OnToggleDoSkipExisting(bool value)
        {
            cacher.DoSkipExistingFiles = value;
        }

        public void OnClearCache()
        {
            string folderPath = cacher.GetLocalMediaPath(imageCacheSubfolder);

            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    File.Delete(file);
                }
            }
        }
        #endregion
    }

}