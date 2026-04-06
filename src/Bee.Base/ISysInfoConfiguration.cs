namespace Bee.Base
{
    /// <summary>
    /// Interface for providing configuration values for <see cref="SysInfo"/>.
    /// </summary>
    public interface ISysInfoConfiguration
    {
        /// <summary>
        /// System major version.
        /// </summary>
        string Version { get; set; }

        /// <summary>
        /// Indicates whether debug mode is enabled.
        /// </summary>
        bool IsDebugMode { get; set; }

        /// <summary>
        /// List of allowed type namespaces for JSON-RPC data transfer (separated by '|').
        /// Only types in these namespaces are allowed for deserialization to ensure security.
        /// </summary>
        string AllowedTypeNamespaces { get; set; }
    }
}
