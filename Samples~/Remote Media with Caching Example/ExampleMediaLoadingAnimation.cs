namespace rlmg.Tools.ContentLoading.Examples
{
    using TMPro;
    using UnityEngine;

    public class ExampleMediaLoadingAnimation : MonoBehaviour
    {
        [SerializeField] private GameObject spinner;
        [SerializeField] private TMP_Text textDisplay;

        ExampleRemoteMediaLoaderWithCaching loader;

        private void Awake()
        {
            loader = FindAnyObjectByType<ExampleRemoteMediaLoaderWithCaching>();
        }

        private void Update()
        {
            if (loader.IsLoadingMedia && !spinner.activeInHierarchy)
                spinner.SetActive(true);
            else if (!loader.IsLoadingMedia && spinner.activeInHierarchy)
                spinner.SetActive(false);

            if (loader.IsLoadingMedia && spinner.activeInHierarchy)
            {
                spinner.transform.Rotate(
                    0f,
                    0f,
                    -360f * Time.deltaTime);
            }

            if (loader.IsLoadingMedia)
                textDisplay.text = "Loading media...";
            else
                textDisplay.text = "";
        }
    }

}