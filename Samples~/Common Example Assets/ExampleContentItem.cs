namespace rlmg.Tools.ContentLoading.Examples
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class ExampleContentItem : MonoBehaviour
    {
        [SerializeField] TMP_Text title, description;
        [SerializeField] RawImage image;

        public void Setup(string title, string description, Texture2D image)
        {
            this.title.text = title;
            this.description.text = description;
            this.image.texture = image;
        }

    }

}