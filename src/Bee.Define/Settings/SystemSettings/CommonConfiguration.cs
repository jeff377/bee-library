using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// Common parameters and environment settings.
    /// </summary>
    [Serializable]
    [XmlType("CommonConfiguration")]
    [Description("Common parameters and environment settings.")]
    [TreeNode("Common")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class CommonConfiguration : IObjectSerializeBase
    {
        /// <summary>
        /// System major version.
        /// </summary>
        [Description("System major version.")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether debug mode is enabled.
        /// </summary>
        [Description("Indicates whether debug mode is enabled.")]
        [DefaultValue(false)]
        public bool IsDebugMode { get; set; } = false;

        /// <summary>
        /// List of allowed type namespaces for JSON-RPC data transfer (separated by '|').
        /// Only types in these namespaces are allowed for deserialization to ensure security.
        /// Example: Custom.Module|ThirdParty.Dto
        /// Note: Bee.Base and Bee.Define are built-in system namespaces and do not need to be specified.
        /// </summary>
        [Category("API")]
        [Description("List of allowed type namespaces for JSON-RPC data transfer, separated by '|'.")]
        [DefaultValue("")]
        public string AllowedTypeNamespaces { get; set; } = string.Empty;

        /// <summary>
        /// Provides API payload handling options, such as serialization, compression, and encryption.
        /// </summary>
        [Category("API")]
        [Description("Provides API payload handling options, such as serialization, compression, and encryption.")]
        public ApiPayloadOptions ApiPayloadOptions { get; set; } = new ApiPayloadOptions();

        /// <summary>
        /// Initialization.
        /// </summary>
        public void Initialize()
        {
            SysInfo.Version = Version;
            SysInfo.IsDebugMode = IsDebugMode;
            // Parse allowed type namespaces list
            SysInfo.AllowedTypeNamespaces = BuildAllowedTypeNamespaces(AllowedTypeNamespaces);
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
                "Bee.Define"
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

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
