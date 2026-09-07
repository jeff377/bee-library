namespace Bee.LoadTests.Configuration
{
    /// <summary>
    /// How requests are paced.
    /// </summary>
    public enum LoadModel
    {
        /// <summary>
        /// Each worker issues its next request only after the previous one returns. Send rate
        /// therefore drops as the system slows, which hides the tail-latency degradation an open
        /// model would expose — but it does mirror how a user of a business application behaves.
        /// </summary>
        Closed = 0,

        /// <summary>
        /// Requests are issued at a fixed arrival rate regardless of how fast responses come back.
        /// This is the model that finds a saturation point.
        /// </summary>
        Open = 1
    }
}
