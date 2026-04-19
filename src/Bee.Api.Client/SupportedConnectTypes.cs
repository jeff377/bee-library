namespace Bee.Api.Client
{
    /// <summary>
    /// Service connection types supported by the application.
    /// </summary>
    [Flags]
    public enum SupportedConnectTypes
    {
        /// <summary>
        /// Local connection (in-process).
        /// </summary>
        Local = 1,
        /// <summary>
        /// Remote connection (over network).
        /// </summary>
        Remote = 2,
        /// <summary>
        /// Both local and remote connections are supported.
        /// </summary>
        Both = Local | Remote
    }
}
