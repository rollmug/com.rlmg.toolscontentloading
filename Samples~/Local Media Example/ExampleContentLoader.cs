namespace rlmg.Tools.ContentLoading.Examples
{
    using System.Collections;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Networking;

    public class ExampleContentLoader : ContentLoader
    {
        [SerializeField] string imagesSubfolder = "images";

        string imagesSubfolderPath
        {
            get
            {
                string directoryName = string.IsNullOrEmpty(imagesSubfolder) ? "images" : imagesSubfolder;
                return Path.Combine(LocalContentDirectory, directoryName);
            }
        }

        public ExampleContentData Data;

        protected override IEnumerator OnLocalSuccess(UnityWebRequest webRequest)
        {
            // Replace Data with new object
            Data = JsonUtility.FromJson<ExampleContentData>(webRequest.downloadHandler.text);

            if (Data == null) yield break;

            if (Data.items == null) yield break;

            // Load each image in the list of images. Note that this is a very basic implementation.
            foreach (ExampleContentItemData item in Data.items)
            {
                if (string.IsNullOrEmpty(item.image)) continue;

                string filepath = Path.Combine(imagesSubfolderPath, item.image);

                // We supply a callback here because coroutines don't have return values.
                yield return MediaLoadingUtility.LoadTextureCoroutine(filepath, tex =>
                {
                    item.cachedImage = tex;
                });
            }
        }
    }

}