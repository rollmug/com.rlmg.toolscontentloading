namespace rlmg.Tools.ContentLoading
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;

    /// <summary>
    /// Read-only UI display of a <see cref="ContentLoader"/>'s last load outcome: a status Image colored
    /// per <see cref="LoadStatus"/> and an optional TMP text summary. Reads the loader's current state
    /// (<see cref="ContentLoader.CurrentStatus"/>/<see cref="ContentLoader.CurrentStatusMessage"/>) on
    /// enable, so it's correct right away even if enabled after loading has finished, then refreshes on
    /// each of the base ContentLoader UnityEvents. Works for any ContentLoader subclass - e.g. a
    /// GraphQLLoader running a one-shot startup query.
    /// </summary>
    public class ContentLoaderStatusDisplay : MonoBehaviour
    {
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

        private void OnEnable()
        {
            if (contentLoader == null)
                return;

            contentLoader.AllLoadingStarting.AddListener(OnAllLoadingStarting);
            contentLoader.AnyLoadSucceeded.AddListener(OnAnyLoadSucceeded);
            contentLoader.AnyLoadFailed.AddListener(OnAnyLoadFailed);
            contentLoader.AllLoadingFinished.AddListener(OnAllLoadingFinished);

            RefreshDisplay();
        }

        private void OnDisable()
        {
            if (contentLoader == null)
                return;

            contentLoader.AllLoadingStarting.RemoveListener(OnAllLoadingStarting);
            contentLoader.AnyLoadSucceeded.RemoveListener(OnAnyLoadSucceeded);
            contentLoader.AnyLoadFailed.RemoveListener(OnAnyLoadFailed);
            contentLoader.AllLoadingFinished.RemoveListener(OnAllLoadingFinished);
        }

        private void OnAllLoadingStarting()
        {
            RefreshDisplay();
        }

        private void OnAnyLoadSucceeded(UnityWebRequest webRequest)
        {
            RefreshDisplay();
        }

        private void OnAnyLoadFailed(UnityWebRequest webRequest)
        {
            RefreshDisplay();
        }

        private void OnAllLoadingFinished()
        {
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (statusText != null)
                statusText.text = contentLoader.CurrentStatusMessage;

            if (statusImage == null)
                return;

            statusImage.color = contentLoader.CurrentStatus switch
            {
                LoadStatus.Loading => loadingColor,
                LoadStatus.Succeeded => succeededColor,
                LoadStatus.Failed => failedColor,
                _ => notLoadedColor,
            };
        }
    }
}
