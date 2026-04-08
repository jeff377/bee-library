namespace Bee.Core.BackgroundServices
{
    /// <summary>
    /// Background service execution action.
    /// </summary>
    public enum BackgroundServiceAction
    {
        /// <summary>
        /// Initialize.
        /// </summary>
        Initialize,
        /// <summary>
        /// Start.
        /// </summary>
        Start,
        /// <summary>
        /// Run.
        /// </summary>
        Run,
        /// <summary>
        /// Stop.
        /// </summary>
        Stop
    }
}
