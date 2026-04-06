namespace Bee.Base.BackgroundServices
{
    /// <summary>
    /// Background service status.
    /// </summary>
    public enum BackgroundServiceStatus
    {
        /// <summary>
        /// Stopped.
        /// </summary>
        Stopped,
        /// <summary>
        /// Starting up.
        /// </summary>
        StartPending,
        /// <summary>
        /// Stopping.
        /// </summary>
        StopPending,
        /// <summary>
        /// Running.
        /// </summary>
        Running
    }
}
