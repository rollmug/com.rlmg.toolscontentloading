namespace rlmg.Tools.ContentLoading
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.Networking;

    /// <summary>
    /// ContentLoader subclass for loading data from a csv file.
    /// </summary>
    /// <remarks>This is an abstract class. See ExampleCSVLoader for implementation tips.</remarks>
    /// <remarks>Assumes there is a header row.</remarks>
    public abstract class CSVLoader<T> : ContentLoader where T : new()
    {
        /// <summary>
        /// String representation of the rows in the file.
        /// Keys are the header row.
        /// </summary>
        public List<T> Rows { get; protected set; }
        
        /// <summary>
        /// Override for CSV parsing. The base method does nothing.
        /// </summary>
        /// <param name="webRequest"></param>
        /// <returns></returns>
        protected override IEnumerator AfterAnySuccess(UnityWebRequest webRequest)
        {
            Rows = CSVMapper.Parse<T>(webRequest.downloadHandler.text);
            yield break;
        }
    }

}