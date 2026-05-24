namespace rlmg.Tools.ContentLoading.Examples
{
    using System;
    using UnityEngine;

    [Serializable]
    public class ExampleContentData
    {
        public ExampleContentItemData[] items;
    }

    [Serializable]
    public class ExampleContentItemData
    {
        public string title;

        public string description;

        /// <summary>
        /// filename
        /// </summary>
        public string image;

        /// <summary>
        /// NonSerialized this field so that we don't attempt to read or write it from/to json
        /// </summary>
        [NonSerialized]
        public Texture2D cachedImage;
    }
}