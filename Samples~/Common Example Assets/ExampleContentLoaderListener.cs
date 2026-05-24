namespace rlmg.Tools.ContentLoading.Examples
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.Networking;

    /// <summary>
    /// Just displays text based on response - success/fail and json if succeeded
    /// </summary>
    public class ExampleContentLoaderListener : MonoBehaviour
    {
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
            contentLoader.AllLoadingStarting.AddListener(OnLoadStarted);
            contentLoader.AnyLoadSucceeded.AddListener(OnLoadSucceeded);
            contentLoader.AnyLoadFailed.AddListener(OnLoadFailed);
        }

        private void OnDisable()
        {
            contentLoader.AllLoadingStarting.RemoveListener(OnLoadStarted);
            contentLoader.AnyLoadSucceeded.RemoveListener(OnLoadSucceeded);
            contentLoader.AnyLoadFailed.RemoveListener(OnLoadFailed);
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