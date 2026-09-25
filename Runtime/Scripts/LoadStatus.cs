namespace rlmg.Tools.ContentLoading
{
    /// <summary>
    /// Current state of an <see cref="ILoadingProgressTracker"/>, readable at any time via
    /// <see cref="ILoadingProgressTracker.CurrentStatus"/>.
    /// </summary>
    public enum LoadStatus
    {
        NotYetLoaded = 0,
        Loading = 1,
        Succeeded = 2,
        Failed = 3,
    }
}
