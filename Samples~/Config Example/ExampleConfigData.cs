namespace rlmg.Tools.ContentLoading.Examples
{
    using UnityEngine;

    /// <summary>
    /// Example ConfigData class
    /// </summary>
    [System.Serializable]
    public class ConfigData
    {
        /// <summary>
        /// Example field - will be read by ContentLoader but not used
        /// </summary>
        public int attractTimeout = 30;

        /// <summary>
        /// Example field - will be read by ContentLoader but not used
        /// </summary>
        public Vector2 screenResolution = new Vector2(1920, 1080);
    }

}