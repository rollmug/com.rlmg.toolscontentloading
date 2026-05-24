namespace rlmg.Tools.ContentLoading
{
    using System;
    using System.Collections;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Networking;

    public class ContentLoader : MonoBehaviour
	{
        /// <summary>
        /// Location where local content file (e.g. json) and mediaCache folder will be saved.
        /// </summary>
        [Serializable]
        protected enum ContentLocationRoot
        {
            StreamingAssets = 0,
            Desktop = 1,
            Application = 2 // see https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Application-dataPath.html for where Application.dataPath points to
        }

        /// <summary>
        /// If true, LoadContent is called on Awake
        /// </summary>
        [SerializeField]
		protected bool loadOnAwake = true;

		/// <summary>
        /// If false, LoadContentCoroutine completes without calling the priority or default local loading methods.
        /// </summary>
		[SerializeField]
		protected bool doLoadContent = true;

		/// <summary>
        /// If Editor and true, LoadContentCoroutine completes
        /// </summary>
		[SerializeField]
		protected bool canUseInEditor = true;

		/// <summary>
        /// If true, LoadContentCoroutine loads local content only
        /// </summary>
		public bool DoLoadLocalContentOnly = false;

        /// <summary>
        /// Filename where local content file (e.g. json) will be written, and read from by default
        /// </summary>
		[SerializeField]
        protected string contentFileName = "content.json";

        /// <summary>
        /// Determines where local content file (e.g. json) and mediaCache folder will be saved.
        /// </summary>
        [SerializeField]
        protected ContentLocationRoot contentLocationRoot = ContentLocationRoot.StreamingAssets;

        /// <summary>
        /// Name of subdirectory where local content file (e.g. json) and mediaCache folder will be saved within the contentLocation.
        /// </summary>
        [SerializeField]
        protected string ContentDirName = "RLMGExternalData";

        /// <summary>
        /// Is set to true after loading the content the first time.
        /// </summary>
        public bool DidLoadSucceed = false;

		/// <summary>
		/// Callback event at start of LoadContentCoroutine
		/// </summary>
		public UnityEvent AllLoadingStarting;

		/// <summary>
		/// 
		/// </summary>
		public UnityEvent<UnityWebRequest> AnyLoadSucceeded;

		/// <summary>
		/// 
		/// </summary>
		public UnityEvent<UnityWebRequest> AnyLoadFailed;

        /// <summary>
        /// Callback event at end of LoadContentCouroutine
        /// whether or not load was successful
        /// </summary>
        /// 
        public UnityEvent AllLoadingFinished;

        /// <summary>
        /// Path to directory where local content file (e.g. json) and mediaCache folder will be saved.
        /// Based on contentLocation. Either StreamingAssets, Desktop, or Application folders.
        /// </summary>
        public string LocalContentDirectory
		{
			get
			{
				string path = "";

                switch (contentLocationRoot)
                {
                    case ContentLocationRoot.StreamingAssets:
                        path = Application.streamingAssetsPath;
                        break;
                    case ContentLocationRoot.Desktop:
                        path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        break;
                    case ContentLocationRoot.Application:
                        path = Path.Combine(Application.dataPath, "..");
                        break;
                }

				if (!string.IsNullOrEmpty(ContentDirName))
				{
					path = Path.Combine(path, ContentDirName);
				}

				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);

				return path;
			}
		}

		/// <summary>
		/// Path to file where local content (e.g. json) will be saved.
		/// </summary>
		protected string localContentPath
        {
			get
            {
				return Path.Combine(LocalContentDirectory, contentFileName);
			}
        }

        protected virtual void Awake()
		{
            // Do loading
            if (loadOnAwake)
				LoadContent();
		}

        #region Load Content Main Methods
        /// <summary>
		/// Main method for loading content. Starts main LoadContentCoroutine.
		/// </summary>
		public virtual void LoadContent()
		{
			StopAllCoroutines();
			StartCoroutine(LoadContentCoroutine());
		}

		
		/// <summary>
		/// Main coroutine for loading content.
		/// Either loads via the priority method or the default, local method.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerator LoadContentCoroutine()
		{
			DidLoadSucceed = false;

			AllLoadingStarting?.Invoke();

			if (doLoadContent && (!Application.isEditor || canUseInEditor))
			{
				if (DoLoadLocalContentOnly)
					yield return StartCoroutine(LoadLocalContent());
				else
					yield return StartCoroutine(MainLoadContent());
			}

			AllLoadingFinished?.Invoke();
		}
		#endregion

		#region Load Target Content
		/// <summary>
		/// The priority method for loading content that will be called first.
		/// Subclasses should define their own success and failure callbacks,
		/// and use the LoadLocalContent as a fallback in the failure callback.
		/// 
		/// Override to use a custom loading method (e.g. via graphql)
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator MainLoadContent()
        {
			yield return StartCoroutine(LoadLocalContent());
        }
		#endregion

		#region Load Local Content
		/// <summary>
		/// Load content via UnityWebRequest from LocalContentPath
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator LoadLocalContent()
        {
			using (UnityWebRequest webRequest = UnityWebRequest.Get(localContentPath))
			{
				yield return webRequest.SendWebRequest();

				switch (webRequest.result)
				{
					case UnityWebRequest.Result.ConnectionError:
					case UnityWebRequest.Result.DataProcessingError:
					case UnityWebRequest.Result.ProtocolError:
                        DidLoadSucceed = false;
                        yield return OnLocalFailure(webRequest.error, webRequest.url);
						AnyLoadFailed?.Invoke(webRequest);
						break;
					case UnityWebRequest.Result.Success:
                        DidLoadSucceed = true;
                        yield return OnLocalSuccess(webRequest);
						yield return AfterAnySuccess(webRequest);
                        AnyLoadSucceeded?.Invoke(webRequest);
                        break;
				}

				yield return LocalFinally(webRequest);
			}
		}

		/// <summary>
		/// Callback for load local success
		/// example body with JsonUtility:		Data = JsonUtility.FromJson<ConfigData>(webRequest.downloadHandler.text);
		/// example body with Newtonsoft.Json:	Data = JsonConvert.DeserializeObject<ConfigData>(webRequest.downloadHandler.text);
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		protected virtual IEnumerator OnLocalSuccess(UnityWebRequest webRequest)
		{
			yield break;
        }

		/// <summary>
		/// Callback for load local error
		/// </summary>
		/// <param name="error"></param>
		/// <param name="url"></param>
		/// <returns></returns>
		protected virtual IEnumerator OnLocalFailure(string error, string url)
        {
			Debug.LogError("Load Local Content Error: " + error + " at " + url);
			yield break;
        }

		/// <summary>
		/// Callback for load local finally.
		/// For when you need to do something regardless of load success or failure.
		/// </summary>
		/// <param name="result"></param>
		/// <returns></returns>
		protected virtual IEnumerator LocalFinally(UnityWebRequest webRequest)
        {
			yield break;
        }
        #endregion

        /// <summary>
        /// Callback for working with loaded data regardless of load approach.
        /// Callback that should be invoked after the load-approach specific success method.
        /// 
        /// e.g.
        /// yield return OnLocalSuccess(webRequest); // Load-approach specific success method for local loading.
        /// yield return OnAnySuccess(webRequest);
        ///
        /// 
        /// example body with JsonUtility:		Data = JsonUtility.FromJson<ConfigData>(webRequest.downloadHandler.text);
        /// example body with Newtonsoft.Json:	Data = JsonConvert.DeserializeObject<ConfigData>(webRequest.downloadHandler.text);
        /// </summary>
        /// <param name="webRequest"></param>
        /// <returns></returns>
        protected virtual IEnumerator AfterAnySuccess(UnityWebRequest webRequest)
		{
			yield break;
		}
    }
}


