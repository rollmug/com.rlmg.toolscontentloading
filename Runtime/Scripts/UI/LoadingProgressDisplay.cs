namespace rlmg.Tools.ContentLoading
{
    using UnityEngine;
    using UnityEngine.UI;

    public class LoadingProgressDisplay : MonoBehaviour
    {
        [SerializeField]
        protected Slider slider;

        // Expose a generic Unity Object field in the Inspector
        [SerializeField]
        protected GameObject trackable;

        protected ILoadingProgressTracker trackableTarget;

        // public getter that safely casts to ILoadingProgressTracker
        protected virtual ILoadingProgressTracker TrackableTarget
        {
            get
            {
                if (trackableTarget == null)
                    trackableTarget = trackable.GetComponent<ILoadingProgressTracker>();

                return trackableTarget;
            }
        }

        // Enforce the interface validation inside the Editor
        protected virtual void OnValidate()
        {
            if (trackable != null && trackable.GetComponent<ILoadingProgressTracker>() == null)
            {
                Debug.LogError($"{trackable.name} does not implement ILoadingProgressTracker!");
                trackable = null; // Rejects the assignment
            }
        }

        protected virtual void Awake()
        {
            if (slider == null)
                slider = GetComponent<Slider>();
        }

        protected virtual void Start()
        {
            if (slider != null)
            {
                slider.minValue = 0;
                slider.maxValue = 1;
                slider.wholeNumbers = false;
            }
        }

        protected virtual void Update()
        {
            if (TrackableTarget == null)
                return;

            slider.value = TrackableTarget.LoadingProgress;
        }
    }

}