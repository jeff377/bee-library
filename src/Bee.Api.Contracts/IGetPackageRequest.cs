namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for get package request parameters.
    /// </summary>
    public interface IGetPackageRequest
    {
        /// <summary>
        /// Gets the application identifier.
        /// </summary>
        string AppId { get; }

        /// <summary>
        /// Gets the component identifier.
        /// </summary>
        string ComponentId { get; }

        /// <summary>
        /// Gets the requested version.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the target platform.
        /// </summary>
        string Platform { get; }

        /// <summary>
        /// Gets the update channel.
        /// </summary>
        string Channel { get; }

        /// <summary>
        /// Gets the specific file identifier.
        /// </summary>
        string FileId { get; }
    }
}
