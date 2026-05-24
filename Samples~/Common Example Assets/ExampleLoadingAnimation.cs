namespace rlmg.Tools.ContentLoading.Examples
{
    using UnityEngine;
    using TMPro;

    // test
    public class ExampleLoadingAnimation : MonoBehaviour
    {
        [SerializeField] private GameObject spinner;
        [SerializeField] private TMP_Text textDisplay;

        ContentCacherAsync cacher;

        private void Awake()
        {
            cacher = FindAnyObjectByType<ContentCacherAsync>();
        }

        private void Update()
        {
            if (cacher.IsCaching && !spinner.activeInHierarchy)
                spinner.SetActive(true);
            else if (!cacher.IsCaching && spinner.activeInHierarchy)
                spinner.SetActive(false);

            if (cacher.IsCaching && spinner.activeInHierarchy)
            {
                spinner.transform.Rotate(
                    0f,
                    0f,
                    -360f * Time.deltaTime);
            }

            if (cacher.IsCaching)
                textDisplay.text = "Caching...";
            else
                textDisplay.text = "";
        }
    }

}