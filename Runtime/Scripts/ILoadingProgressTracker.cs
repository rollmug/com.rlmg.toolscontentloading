namespace rlmg.Tools.ContentLoading
{
    public interface ILoadingProgressTracker
    {
        public float LoadingProgress { get; }
        public bool IsLoading { get; }
    }
}