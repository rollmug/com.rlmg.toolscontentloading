namespace rlmg.Tools.ContentLoading.Examples
{
    using System.Collections.Generic;
    using System.Collections;
    using UnityEngine;
    using TMPro;

    /// <summary>
    /// This example shows how multiple ContentLoaders provide a public Coroutine for sequentially executing their loading.
    /// </summary>
    /// <remarks>Delays loading between loaders for example purposes only.</remarks>
    public class ExampleSequentialLoader : MonoBehaviour
    {
        /// <summary>
        /// Load all loaders on awake?
        /// </summary>
        [SerializeField] bool loadOnAwake = true;

        /// <summary>
        /// How many seconds to wait between loaders.
        /// </summary>
        [SerializeField] float waitBetweenLoaders = 5f;

        /// <summary>
        /// Wait between loaders.
        /// </summary>
        WaitForSeconds wait;

        /// <summary>
        /// ContentLoaders to load sequentially.
        /// </summary>
        [SerializeField] List<ContentLoader> loaders;

        /// <summary>
        /// Display fields - cooperatively managed with ExampleContentListeners in this example.
        /// </summary>
        [SerializeField] TMP_Text[] statusDisplays, jsonDisplays;

        /// <summary>
        /// Start loading if set to do so.
        /// </summary>
        private void Awake()
        {
            if (loadOnAwake)
            {
                StopAllCoroutines();
                StartCoroutine(LoadAll());
            }  
        }

        /// <summary>
        /// Load loaders sequentially.
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadAll()
        {
            foreach (var status in statusDisplays)
                status.text = "Loading not yet started!";

            foreach (var jsonDisplay in jsonDisplays)
                jsonDisplay.text = "...";

            wait = new WaitForSeconds(waitBetweenLoaders);

            for (int i = 0; i < loaders.Count; i++)
            {
                ContentLoader loader = loaders[i];
                yield return loader.LoadContentCoroutine();

                Debug.Log(loader.name + " done loading!");

                if (i < loaders.Count - 1)
                    yield return wait;
            }

            Debug.Log("Done loading all loaders.");
        }

        /// <summary>
        /// Callback for button click.
        /// </summary>
        public void OnReload()
        {
            StopAllCoroutines();
            StartCoroutine(LoadAll());
        }
    }
}
