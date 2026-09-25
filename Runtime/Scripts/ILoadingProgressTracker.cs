namespace rlmg.Tools.ContentLoading
{
    public interface ILoadingProgressTracker
    {
        public float LoadingProgress { get; }

        public LoadStatus CurrentStatus { get; }
    }
}
