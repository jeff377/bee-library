namespace Bee.Api.Client
{
    /// <summary>
    /// Service connection type.
    /// </summary>
    public enum ConnectType
    {
        /// <summary>
        /// Local connection (in-process).
        /// </summary>
        Local,
        /// <summary>
        /// Remote connection (over network).
        /// </summary>
        Remote
    }
}
