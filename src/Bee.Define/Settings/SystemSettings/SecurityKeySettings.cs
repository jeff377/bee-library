using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// Encryption key settings for storing encryption information in configuration files.
    /// </summary>
    [Serializable]
    [XmlType("SecurityKeySettings")]
    [Description("Encryption key settings.")]
    [TreeNode("Security Keys")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class SecurityKeySettings
    {
        /// <summary>
        /// Master key source.
        /// </summary>
        [Description("Master key source.")]
        public MasterKeySource MasterKeySource { get; set; } = new MasterKeySource();

        /// <summary>
        /// API transport key (encrypted with master key, base64 string).
        /// </summary>
        [Description("API transport key (encrypted with master key, base64 string).")]
        public string ApiEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// Cookie key (encrypted with master key, base64 string).
        /// </summary>
        [Description("Cookie key (encrypted with master key, base64 string).")]
        public string CookieEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// Config sensitive data encryption key (encrypted with master key, base64 string).
        /// </summary>
        [Description("Encryption key for sensitive data in config files (encrypted with master key, base64 string).")]
        public string ConfigEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// Database sensitive field encryption key (encrypted with master key, base64 string).
        /// </summary>
        [Description("Encryption key for sensitive fields in the database (encrypted with master key, base64 string).")]
        public string DatabaseEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }

}
