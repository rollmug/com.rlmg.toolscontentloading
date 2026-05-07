namespace rlmg.Tools.ContentLoading.Directus
{
    using System;

    [Serializable]
    public class DirectusFile
    {
        #region Graph Properties
        /// <summary>
        /// This is the one you want to use for downloading the file, as it is the hashed filename that Directus uses for storage.
        /// </summary>
        public string filename_download { get; set; }

        /// <summary>
        /// This is the human readable filename, which may be useful for display purposes, but is not the one to use for downloading the file, as Directus does not use it for storage.
        /// </summary>
        public string filename_disk { get; set; }

        public string description { get; set; }
        #endregion

        #region NonSerialized Fields
        /// <summary>
        /// For ContentLoader to store the remote path for this file.
        /// </summary>
        [NonSerialized]
        public string PathDownload;

        /// <summary>
        /// For ContentLoader to store the local path for this file after downloading.
        /// </summary>
        [NonSerialized]
        public string LocalPath;
        #endregion
    }
}


