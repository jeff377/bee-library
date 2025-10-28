using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// Backend parameters and environment settings.
    /// </summary>
    [Serializable]
    [XmlType("BackendConfiguration")]
    [Description("Backend parameters and environment settings.")]
    [TreeNode("Backend")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class BackendConfiguration
    {
        /// <summary>
        /// Database type.
        /// </summary>
        [Category("Database")]
        [Description("Database type.")]
        [DefaultValue(DatabaseType.SQLServer)]
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;

        /// <summary>
        /// Default database ID.
        /// </summary>
        [Category("Database")]
        [Description("Default database ID.")]
        [DefaultValue("")]
        public string DatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// Maximum DbCommand timeout (seconds). 
        /// Default is 60 seconds. Set to 0 for unlimited.
        /// </summary>
        [Category("Database")]
        [Description("Maximum DbCommand timeout (seconds). Default is 60 seconds. Set to 0 for unlimited.")]
        [DefaultValue(60)]
        public int MaxDbCommandTimeout { get; set; } = 60;

        /// <summary>
        /// Logging options for configuring log parameters.
        /// </summary>
        [Category("Logging")]
        [Description("Provides logging options, such as log level and output format.")]
        [Browsable(false)]
        public LogOptions LogOptions { get; set; } = new LogOptions();

        /// <summary>
        /// API KEY.
        /// </summary>
        [Category("API")]
        [Description("API KEY.")]
        [DefaultValue("")]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Encryption key settings.
        /// </summary>
        [Category("Security")]
        [Description("Encryption key settings.")]
        [Browsable(false)]
        public SecurityKeySettings SecurityKeySettings { get; set; } = new SecurityKeySettings();

        /// <summary>
        /// 後端可替換組。
        /// </summary>
        [Category("Components")]
        [Description("後端可替換組")]
        [Browsable(false)]
        public BackendComponents Components { get; set; } = new BackendComponents();

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
