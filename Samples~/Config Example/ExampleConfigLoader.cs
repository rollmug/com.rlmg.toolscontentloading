namespace rlmg.Tools.ContentLoading.Examples
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Networking;

    /// <summary>
    /// Example of subclassing ContentLoader
    /// </summary>
    public class ExampleConfigLoader : ContentLoader
    {
        /// <summary>
        /// For access by other MonoBehaviours, as needed
        /// </summary>
        public ConfigData Data;

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

            Data = JsonUtility.FromJson<ConfigData>(webRequest.downloadHandler.text);

            // One possible design would be to initialize configurable components here.
            // e.g. AttractManager.timeout = Data.attractTimeout;
        }
    }
}


