using System;
using System.ComponentModel;
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
    public class CommonConfiguration : IObjectSerializeBase, ISysInfoConfiguration
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
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
