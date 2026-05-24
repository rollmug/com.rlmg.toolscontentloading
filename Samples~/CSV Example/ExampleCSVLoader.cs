namespace rlmg.Tools.ContentLoading.Examples
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Networking;
    using TMPro;

    /// <summary>
    /// Example implementation of abstract base class CSVLoader.
    /// </summary>
    /// <remarks>Implementing this class requires supplying a data class representing each row. In this case, ExampleCSVDataRow is used.</remarks>
    public class ExampleCSVLoader : CSVLoader<ExampleCSVDataRow>
    {
        [Header("Example-Specific Fields")]
        [SerializeField] TMP_Text attractTimeoutDisplay;
        [SerializeField] TMP_Text titleDisplay;
        
        /// <summary>
        /// Simple override to display results in UI.
        /// </summary>
        /// <param name="webRequest"></param>
        /// <returns></returns>
        /// <remarks>The data is successfully deserialized in the base class before being applied to UI.</remarks>
        protected override IEnumerator AfterAnySuccess(UnityWebRequest webRequest)
        {
            yield return base.AfterAnySuccess(webRequest); // parsing happens here
            
            if (Rows.Count > 0)
            {
                attractTimeoutDisplay.text = Rows[0].attractTimeout.ToString();
                titleDisplay.text = Rows[0].title;
            }
        }
    }
}

