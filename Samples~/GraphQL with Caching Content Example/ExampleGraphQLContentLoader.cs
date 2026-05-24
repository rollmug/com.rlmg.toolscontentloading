namespace rlmg.Tools.ContentLoading.Examples
{
    using System.Collections;
    using System.Net;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;

    /// <summary>
    /// GraphQL loader that queries the PokéAPI
    /// </summary>
    /// <remarks>Note the switch statements for GraphQL and REST GET; those endpoints return different json, so there are different data class wrappers.</remarks>
    public class ExampleGraphQLContentLoader : GraphQLLoader
    {
        [Header("Example-Specific Settings")]
        /// <summary>
        /// Safe template for where to retrieve Pokémon images. The PokéAPI's sprite endpoint is complex, so this shortcuts getting the image URL endpoints using a known safe template.
        /// </summary>
        [Tooltip("Safe template for where to retrieve Pokémon images. The PokéAPI's sprite endpoint is complex, so this shortcuts getting the image URL endpoints using a known safe template.")]
        [SerializeField] string imageURL;

        /// <summary>
        /// Made public so other MonoBehaviours can access the data after it's loaded, as needed.
        /// </summary>
        public PokemonSpecies[] Data;

        [Header("UI Elements")]
        [SerializeField] TMP_Dropdown dropdownRequestMode;
        [SerializeField] Toggle toggleUseConfig, toggleUseServer, toggleUseQueryFile, toggleDoCache;
        

        /// <summary>
        /// Just an override to update the UI because there's no callback hook for the loading the config step.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator LoadLoaderConfig()
        {
            yield return base.LoadLoaderConfig();
            UpdateUI();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="webRequest"></param>
        /// <returns></returns>
        protected override IEnumerator AfterAnySuccess(UnityWebRequest webRequest)
        {
            if (webRequest.downloadHandler.text.Contains("data")) // we assume it's in the GraphQL results format
            {
                Debug.Log("Successfully loaded content using GraphQL request!\n" + webRequest.downloadHandler.text);
                Data = JsonUtility.FromJson<PokemonGraphDataWrapper>(webRequest.downloadHandler.text).data.pokemonspecies;
            }
            else if (webRequest.downloadHandler.text.Contains("results")) // we assume it's in the REST Get results format
            {
                Debug.Log("Successfully loaded content using REST GET request!\n" + webRequest.downloadHandler.text);
                Data = JsonUtility.FromJson<PokemonRESTDataWrapper>(webRequest.downloadHandler.text).results;

                // shortcut to getting id - it's at the end of the url field, but not included as its own field in the REST results
                if (Data != null)
                    foreach (PokemonSpecies item in Data)
                    {
                        if (!string.IsNullOrEmpty(item?.url))
                            int.TryParse(item.url.Substring(item.url.Length - 2, 1), out item.id);

                        item.pokemonspeciesflavortexts = new PokemonSpeciesFlavorText[1] { new PokemonSpeciesFlavorText() { flavor_text="REST results have no flavor text." } };
                    }
                        
            }

            if (UseServer)
                yield return DownloadImages();
        }

        /// <summary>
        /// Downloads images for each retrieved pokemon.
        /// 
        /// Because the PokéAPI's sprite endpoint is complex, this shortcuts getting the image URL endpoints
        /// using a known safe template.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DownloadImages()
        {
            if (Data == null)
                yield break;

            foreach (PokemonSpecies item in Data)
            {
                if (item.id == 0)
                    continue;

                // known safe template for where to retrieve Pokemon images
                string imageEndpoint = imageURL + "/" + item.id + ".png";

                yield return MediaLoadingUtility.LoadTextureCoroutine(imageEndpoint,
                    tex =>
                    {
                        // store the Texture2D on a NonSerialized field of the PokemonSpecies data item
                        item.Texture = tex;
                    },
                    (message, uri) =>
                    {
                        Debug.LogError(string.Format("Could not load image at {0}\n{1}\n{2}", imageEndpoint, message, uri));
                    }
                );
            }
        }

        #region Overrides for Debug Statements Only
        /// <summary>
        /// Just an override to add a debug statement.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator MainLoadContent()
        {
            Debug.Log("Attempting remote load...");
            return base.MainLoadContent();
        }

        /// <summary>
        /// Just an override to add a debug statement.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator OnRemoteResponseSuccess(UnityWebRequest webRequest)
        {
            Debug.Log("Remote response successful!");
            return base.OnRemoteResponseSuccess(webRequest);
        }

        /// <summary>
        /// Just an override to add a debug statement.
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator OnLocalSuccess(UnityWebRequest webRequest)
        {
            Debug.Log("Local loading successful!");
            return base.OnLocalSuccess(webRequest);
        }
        #endregion

        #region UI Methods
        private void UpdateUI()
        {
            toggleUseConfig.isOn = doLoadLoaderConfigFromDisk;
            toggleUseServer.isOn = UseServer;
            toggleUseQueryFile.isOn = doLoadQueryFromDisk;
            toggleDoCache.isOn = cacher.DoCacheText;
            dropdownRequestMode.value = (int)requestMode;
        }

        /// <summary>
        /// Public method to toggle caching on/off. Caching is on by default, but you might want to turn it off for testing purposes.
        /// </summary>
        /// <param name="value"></param>
        public void ToggleCaching(bool value)
        {
            cacher.DoCacheText = value;
        }

        /// <summary>
        /// Public method to toggle using the server on/off. Using the server is on by default, but you might want to turn it off to test server loading and caching functionality.
        /// </summary>
        /// <param name="value"></param>
        public void ToggleUseServer(bool value)
        {
            UseServer = value;
        }

        /// <summary>
        /// Public method to toggle using the config on/off. Using the config is on by default, but you might want to turn it off to test loading from disk functionality.
        /// </summary>
        /// <param name="value"></param>
        public void ToggleUseConfig(bool value)
        {
            doLoadLoaderConfigFromDisk = value;
        }

        /// <summary>
        /// Public method to toggle loading the query file from disk on/off. Loading from disk is on by default, but you might want to turn it off to test loading from the server functionality without the fallback to loading from disk if the server load fails.
        /// </summary>
        /// <param name="value"></param>
        public void ToggleLoadQueryFile(bool value)
        {
            doLoadQueryFromDisk = value;
        }

        /// <summary>
        /// Callback for request mode dropdown.
        /// </summary>
        /// <param name="value"></param>
        public void OnRequestModeDropdownChange(int value)
        {
            requestMode = (RequestMode)value;
        }
        #endregion
    }

}