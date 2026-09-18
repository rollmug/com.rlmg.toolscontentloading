namespace rlmg.Tools.ContentLoading.Examples
{
    using UnityEngine;
    using UnityEngine.Networking;
    using TMPro;

    public class ExampleSequentialLoaderListener : MonoBehaviour
    {
        [SerializeField] ExampleSequentialLoader sequentialLoader;

        /// <summary>
        /// Any ContentLoader in the scene
        /// </summary>
        [SerializeField] ContentLoader contentLoader;

        [SerializeField] protected TMP_Text heading, body;

        private void Awake()
        {
            if (contentLoader == null)
                contentLoader = FindAnyObjectByType<ContentLoader>();
        }

        private void OnEnable()
        {
            sequentialLoader.LoaderSequenceStarted.AddListener(OnLoaderSequenceStarted);
            contentLoader.AllLoadingStarting.AddListener(OnLoadStarted);
            contentLoader.AnyLoadSucceeded.AddListener(OnLoadSucceeded);
            contentLoader.AnyLoadFailed.AddListener(OnLoadFailed);
        }

        private void OnDisable()
        {
            sequentialLoader.LoaderSequenceStarted.RemoveListener(OnLoaderSequenceStarted);
            contentLoader.AllLoadingStarting.RemoveListener(OnLoadStarted);
            contentLoader.AnyLoadSucceeded.RemoveListener(OnLoadSucceeded);
            contentLoader.AnyLoadFailed.RemoveListener(OnLoadFailed);
        }

        private void OnLoaderSequenceStarted()
        {
            heading.text = "Loading not yet started!";
            body.text = "";
        }

        private void OnLoadStarted()
        {
            heading.text = "Loading...";
            body.text = "";
        }

        protected virtual void OnLoadSucceeded(UnityWebRequest request)
        {
            heading.text = "Loaded successfully!";
            body.text = ContentCacher.PrettifyJson( request.downloadHandler.text );
        }
            
        private void OnLoadFailed(UnityWebRequest request)
        {
            heading.text = "Load failed!";
            body.text = $"An error occurred while loading the content from\n{request.uri}:\n{request.error}";
        }
    }

}