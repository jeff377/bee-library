using System.ComponentModel;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Common parameters and environment settings.
    /// </summary>
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
        /// Default language (BCP-47 specific, e.g. <c>"zh-TW"</c>, <c>"en-US"</c>).
        /// Used as the fall-back when the current session's language has no matching
        /// translation, and as the seed for newly created sessions that did not specify one.
        /// </summary>
        [Description("Default BCP-47 language code (e.g. zh-TW, en-US).")]
        [DefaultValue("en-US")]
        public string DefaultLang { get; set; } = "en-US";

        /// <summary>
        /// List of allowed type namespaces for JSON-RPC data transfer (separated by '|').
        /// Only types in these namespaces are allowed for deserialization to ensure security.
        /// Example: Custom.Module|ThirdParty.Dto
        /// Note: Bee.Base and Bee.Definition are built-in system namespaces and do not need to be specified.
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
