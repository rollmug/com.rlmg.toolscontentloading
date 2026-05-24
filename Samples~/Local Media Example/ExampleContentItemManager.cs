namespace rlmg.Tools.ContentLoading.Examples
{
    using UnityEngine;
    using UnityEngine.Networking;

    public class ExampleContentItemManager : MonoBehaviour
    {
        [SerializeField] GameObject contentItemPrefab;

        [SerializeField] RectTransform contentItemsContainer;

        ExampleContentLoader contentLoader;

        private void Awake()
        {
            contentLoader = FindAnyObjectByType<ExampleContentLoader>();
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
            InstantiateContentItems(contentLoader.Data.items);
        }

        public void ClearAllItems()
        {
            foreach (Transform child in contentItemsContainer)
            {
                Destroy(child.gameObject);
            }
            contentItemsContainer.DetachChildren();
        }

        public void InstantiateContentItems(ExampleContentItemData[] items)
        {
            ClearAllItems();
            if (items == null) return;
            foreach (ExampleContentItemData item in items)
            {
                GameObject contentItemGO = Instantiate(contentItemPrefab, contentItemsContainer);
                ExampleContentItem contentItem = contentItemGO.GetComponent<ExampleContentItem>();
                contentItem.Setup(item.title, item.description, item.cachedImage);
            }
        }
    }

}