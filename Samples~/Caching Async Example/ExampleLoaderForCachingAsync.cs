namespace rlmg.Tools.ContentLoading.Examples
{
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Collections;
    using System.IO;
    using TMPro;

    /// <summary>
    /// Example of how to load from a local file and save its content to disk asynchronously.
    /// </summary>
    [RequireComponent(typeof(ContentCacherAsync))]
    public class ExampleLoaderForCachingAsync : ContentLoader
    {
        /// <summary>
        /// Cacher of which this example shows off the settings.
        /// </summary>
        ContentCacherAsync cacher;

        [Header("Example-Specific Settings")]
        /// <summary>
        /// A separate file destination to show off the caching.
        /// </summary>
        /// <remarks>Normally, we wouldn't need to cache a file that's already on disk.</remarks>
        [SerializeField] private string cacheFileName = "example-cache-async.json";

        [SerializeField] TMP_Dropdown dropdown;

        /// <summary>
        /// Full path to cache destination.
        /// </summary>
        private string localCachePath
        {
            get
            {
                return Path.Combine(LocalContentDirectory, cacheFileName);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            cacher = GetComponent<ContentCacherAsync>();

            if (dropdown != null)
                dropdown.value = cacher.DebugSleepDur;

        }

        /// <summary>
        /// The base method does nothing.
        /// The override gives you the possibility to use the data.
        /// </summary>
        /// <param name="webRequest"></param>
        /// <returns></returns>
        protected override IEnumerator OnLocalSuccess(UnityWebRequest webRequest)
        {
            if (string.IsNullOrEmpty(webRequest?.downloadHandler?.text))
                yield break;

            Debug.Log("Loaded content successfully!");

            cacher.CacheText(webRequest.downloadHandler.text, localCachePath);
        }

        #region UI Methods
        /// <summary>
        /// Change the sleep duration of the cacher - just a wait to prolong the task for debug purposes.
        /// </summary>
        /// <param name="index"></param>
        public void OnDropdownChanged(int index)
        {
            cacher.DebugSleepDur = index;
        }
        #endregion
    }

}