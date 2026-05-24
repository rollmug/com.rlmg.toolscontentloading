namespace rlmg.Tools.ContentLoading.Examples
{
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Manages the creation and display of content items in a Unity UI container based on data loaded via a GraphQL
    /// content loader.
    /// </summary>
    /// <remarks>This component listens for successful data load events from an associated
    /// ExampleGraphQLContentLoader and updates the UI by instantiating content item elements accordingly..</remarks>
    public class PokemonContentItemManager : MonoBehaviour
    {
        [SerializeField] GameObject contentItemPrefab;

        [SerializeField] RectTransform contentItemsContainer;

        ExampleGraphQLContentLoader contentLoader;

        private void Awake()
        {
            contentLoader = FindAnyObjectByType<ExampleGraphQLContentLoader>();
            ClearAllItems();
        }

        private void OnEnable()
        {
            contentLoader.AnyLoadSucceeded.AddListener(OnLoadSucceeded);
        }

        private void OnDisable()
        {
            contentLoader.AnyLoadSucceeded.RemoveListener(OnLoadSucceeded);
        }

        private void OnLoadSucceeded(UnityWebRequest request)
        {
            InstantiateContentItems(contentLoader.Data);
        }

        public void ClearAllItems()
        {
            foreach (Transform child in contentItemsContainer)
            {
                Destroy(child.gameObject);
            }
            contentItemsContainer.DetachChildren();
        }

        public void InstantiateContentItems(PokemonSpecies[] items)
        {
            ClearAllItems();
            if (items == null) return;
            foreach (PokemonSpecies item in items)
            {
                GameObject contentItemGO = Instantiate(contentItemPrefab, contentItemsContainer);
                ExampleContentItem contentItem = contentItemGO.GetComponent<ExampleContentItem>();

                string description = null;
                if (item?.pokemonspeciesflavortexts?.Length > 0)
                {
                    description = item.pokemonspeciesflavortexts[0].flavor_text;

                    // fix some formatting issues in description
                    description = Regex.Replace(description, @"[\f\n]", " ");
                }

                contentItem.Setup(item?.name, description, item?.Texture);
            }
        }
    }

}