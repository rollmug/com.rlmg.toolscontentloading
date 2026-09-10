namespace rlmg.Tools.ContentLoading
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;

    /// <summary>
    /// Read-only UI display of a <see cref="ContentLoader"/>'s last load outcome: a status Image colored
    /// per <see cref="LoadStatus"/> and an optional TMP text summary. Listens to the base ContentLoader
    /// UnityEvents (AllLoadingStarting/AnyLoadSucceeded/AnyLoadFailed) rather than polling, since that
    /// lifecycle is event-driven. Works for any ContentLoader subclass - e.g. a GraphQLLoader running a
    /// one-shot startup query.
    /// </summary>
    public class ContentLoaderStatusDisplay : MonoBehaviour
    {
        private enum LoadStatus
        {
            NotLoaded = 0,
            Loading = 1,
            Succeeded = 2,
            Failed = 3,
        }

        [Header("Content Loader")]
        [SerializeField] private ContentLoader contentLoader;

        [Header("Status Text")]
        [SerializeField] private TMP_Text statusText;

        [Header("Status Image")]
        [SerializeField] private Image statusImage;
        [SerializeField] private Color succeededColor = Color.green;
        [SerializeField] private Color loadingColor = Color.yellow;
        [SerializeField] private Color notLoadedColor = Color.gray;
        [SerializeField] private Color failedColor = Color.red;

        private LoadStatus status = LoadStatus.NotLoaded;
        private string lastMessage = "Not loaded";

        private void OnEnable()
        {
            if (contentLoader == null)
                return;

            contentLoader.AllLoadingStarting.AddListener(OnLoadingStarting);
            contentLoader.AnyLoadSucceeded.AddListener(OnLoadSucceeded);
            contentLoader.AnyLoadFailed.AddListener(OnLoadFailed);

            RefreshDisplay();
        }

        private void OnDisable()
        {
            if (contentLoader == null)
                return;

            contentLoader.AllLoadingStarting.RemoveListener(OnLoadingStarting);
            contentLoader.AnyLoadSucceeded.RemoveListener(OnLoadSucceeded);
            contentLoader.AnyLoadFailed.RemoveListener(OnLoadFailed);
        }

        private void OnLoadingStarting()
        {
            SetStatus(LoadStatus.Loading, "Loading...");
        }

        private void OnLoadSucceeded(UnityWebRequest webRequest)
        {
            SetStatus(LoadStatus.Succeeded, "Succeeded");
        }

        private void OnLoadFailed(UnityWebRequest webRequest)
        {
            SetStatus(LoadStatus.Failed, "Failed: " + webRequest?.error);
        }

        private void SetStatus(LoadStatus newStatus, string message)
        {
            status = newStatus;
            lastMessage = message;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (statusText != null)
                statusText.text = lastMessage;

            if (statusImage == null)
                return;

            statusImage.color = status switch
            {
                LoadStatus.Succeeded => succeededColor,
                LoadStatus.Loading => loadingColor,
                LoadStatus.Failed => failedColor,
                _ => notLoadedColor,
            };
        }
    }
}
