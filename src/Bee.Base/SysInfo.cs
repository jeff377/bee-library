using System;
using System.Collections.Generic;
using System.Linq;
using Bee.Base.Tracing;

namespace Bee.Base
{
    /// <summary>
    /// System information; shared parameters and environment settings for both frontend and backend.
    /// </summary>
    public static class SysInfo
    {
        static SysInfo()
        {
            // Add the default allowed type namespaces for JSON-RPC data transfer
            AllowedTypeNamespaces = new List<string> { "Bee.Base", "Bee.Define", "Bee.Contracts" };
        }

        /// <summary>
        /// Gets or sets the system major version number.
        /// </summary>
        public static string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether tracing is enabled (read-only; enabled when TraceListener is not null).
        /// </summary>
        public static bool TraceEnabled => TraceListener != null;

        /// <summary>
        /// Gets or sets the execution flow monitor, which provides system-level trace segment monitoring.
        /// Called by the application to record the start, end, and individual events in the execution flow,
        /// enabling performance analysis and exception tracing.
        /// </summary>
        public static ITraceListener TraceListener { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether debug mode is enabled.
        /// </summary>
        public static bool IsDebugMode { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the application is running in tool mode (e.g. SettingsEditor.exe).
        /// This property can only be set during application startup and cannot be loaded from a configuration file.
        /// Used to allow local execution without requiring AccessToken authentication.
        /// </summary>
        public static bool IsToolMode { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the application is published as a single-file executable (e.g. SettingsEditor.exe).
        /// When published as a single file, dynamic object loading is not available and objects must be created via code.
        /// </summary>
        public static bool IsSingleFile { get; set; } = false;

        /// <summary>
        /// Gets or sets the list of type namespaces allowed for JSON-RPC data transfer.
        /// Only types in these namespaces are permitted for deserialization to ensure security.
        /// Note: Bee.Base and Bee.Define are built-in default namespaces and do not need to be specified.
        /// </summary>
        public static List<string> AllowedTypeNamespaces { get; set; }

        /// <summary>
        /// Validates whether the specified type name is in an allowed namespace.
        /// </summary>
        /// <param name="typeName">The type name to validate.</param>
        public static bool IsTypeNameAllowed(string typeName)
        {
            foreach (var ns in AllowedTypeNamespaces)
            {
                if (typeName.StartsWith(ns + "."))
                    return true;
            }

            return typeName == "System.Byte[]";
        }

        /// <summary>
        /// Initializes SysInfo with the provided configuration values.
        /// </summary>
        /// <param name="configuration">The configuration values for SysInfo.</param>
        public static void Initialize(ISysInfoConfiguration configuration)
        {
            Version = configuration.Version;
            IsDebugMode = configuration.IsDebugMode;
            AllowedTypeNamespaces = BuildAllowedTypeNamespaces(configuration.AllowedTypeNamespaces);
        }

        /// <summary>
        /// Parse the list of allowed type namespaces (including system default and user-defined).
        /// </summary>
        /// <param name="customNamespaces">User-defined namespace string, separated by '|'.</param>
        /// <returns>List of namespaces including system default and user-defined.</returns>
        public static List<string> BuildAllowedTypeNamespaces(string customNamespaces)
        {
            // Initialize HashSet to ensure no duplicates
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Bee.Base",
                "Bee.Define",
                "Bee.Contracts"
            };

            // User-defined namespace list (separated by '|')
            // User value may be null, empty, or contain extra separators
            if (!string.IsNullOrWhiteSpace(customNamespaces))
            {
                var parts = customNamespaces.Split('|');
                foreach (var ns in parts)
                {
                    var trimmed = ns.Trim().TrimEnd('.');
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        allowed.Add(trimmed);
                    }
                }
            }

            return allowed.ToList();
        }
    }
}
