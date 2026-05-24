namespace rlmg.Tools.ContentLoading.Examples
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEngine;

    /// <summary>
    /// Instantiates items as they are loaded, on a callback from the loader.
    /// </summary>
    public class PokemonRemoteMediaItemManager : MonoBehaviour
    {
        [SerializeField] GameObject contentItemPrefab;

        [SerializeField] RectTransform contentItemsContainer;

        ExampleRemoteMediaLoaderWithCaching contentLoader;

        Dictionary<int, Transform> instantiatedItems;

        private void Awake()
        {
            contentLoader = FindAnyObjectByType<ExampleRemoteMediaLoaderWithCaching>();

            instantiatedItems = new Dictionary<int, Transform>();

            ClearAllItems();
        }

        private void OnEnable()
        {
            contentLoader.AllLoadingStarting.AddListener(OnContentLoadingStarted);
            contentLoader.AllLoadingFinished.AddListener(OnContentLoadingFinished);
            contentLoader.ItemLoaded.AddListener(OnItemLoaded);
        }

        private void OnDisable()
        {
            contentLoader.AllLoadingStarting.RemoveListener(OnContentLoadingStarted);
            contentLoader.AllLoadingFinished.RemoveListener(OnContentLoadingFinished);
            contentLoader.ItemLoaded.RemoveListener(OnItemLoaded);
        }

        private void OnContentLoadingStarted()
        {
            ClearAllItems();
        }

        private void OnItemLoaded(PokemonSpecies item)
        {
            InstantiateContentItem(item);
        }

        private void OnContentLoadingFinished()
        {
            SortItems();
        }

        public void ClearAllItems()
        {
            foreach (Transform child in contentItemsContainer)
            {
                Destroy(child.gameObject);
            }
            contentItemsContainer.DetachChildren();

            instantiatedItems.Clear();
        }

        private void InstantiateContentItem(PokemonSpecies item)
        {
            GameObject contentItemGO = Instantiate(contentItemPrefab, contentItemsContainer);
            ExampleContentItem contentItem = contentItemGO.GetComponent<ExampleContentItem>();

            instantiatedItems[item.id] = contentItemGO.transform;

            string description = null;
            if (item?.pokemonspeciesflavortexts?.Length > 0)
            {
                description = item.pokemonspeciesflavortexts[0].flavor_text;

                // fix some formatting issues in description
                description = Regex.Replace(description, @"[\f\n]", " ");
            }

            contentItem.Setup(item?.name, description, item?.Texture);
        }

        private void SortItems()
        {
            List<int> ids = instantiatedItems.Keys.ToList();
            ids.Sort();
            foreach (int id in ids)
                instantiatedItems[id].SetSiblingIndex(id);
        }
    }

}