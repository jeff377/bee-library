using Bee.Definition.Logging;
using System.ComponentModel;
using Bee.Base.Attributes;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Backend parameters and environment settings.
    /// </summary>
    [Description("Backend parameters and environment settings.")]
    [TreeNode("Backend")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class BackendConfiguration
    {
        /// <summary>
        /// Logging options for configuring log parameters.
        /// </summary>
        [Category("Logging")]
        [Description("Provides logging options, such as log level and output format.")]
        [Browsable(false)]
        public LogOptions LogOptions { get; set; } = new LogOptions();

        /// <summary>
        /// Encryption key settings.
        /// </summary>
        [Category("Security")]
        [Description("Encryption key settings.")]
        [Browsable(false)]
        public SecurityKeySettings SecurityKeySettings { get; set; } = new SecurityKeySettings();

        /// <summary>
        /// Backend replaceable components.
        /// </summary>
        [Category("Components")]
        [Description("Backend replaceable components.")]
        [Browsable(false)]
        public BackendComponents Components { get; set; } = new BackendComponents();

        /// <summary>
        /// Cache-notify poller options (database-backed cache invalidation).
        /// </summary>
        [Category("CacheNotify")]
        [Description("Cache-notify poller options for database-backed cache invalidation.")]
        [Browsable(false)]
        public CacheNotifyOptions CacheNotifyOptions { get; set; } = new CacheNotifyOptions();

        /// <summary>
        /// Audit-trail (data-history) logging options. Disabled by default.
        /// </summary>
        [Category("AuditLog")]
        [Description("Audit-trail (data-history) logging options.")]
        [Browsable(false)]
        public AuditLogOptions AuditLogOptions { get; set; } = new AuditLogOptions();

        /// <summary>
        /// Gets or sets the IANA time zone id applied to a session when the user has no
        /// <c>st_user.time_zone</c> of their own. An empty value means UTC.
        /// </summary>
        /// <remarks>
        /// The default is <c>Asia/Taipei</c> rather than empty for backward compatibility: the
        /// <c>st_user.time_zone</c> column was introduced in this release, so every existing row is
        /// blank, and defaulting to UTC would shift all of their displayed times on upgrade.
        /// Deployments outside that zone should set this explicitly — or set it to an empty string
        /// to opt into UTC, which is what the conversion layer already does for a blank zone.
        /// </remarks>
        [Category("Localization")]
        [Description("IANA time zone id applied when a user has no time zone of their own. Empty means UTC.")]
        [DefaultValue("Asia/Taipei")]
        public string DefaultTimeZone { get; set; } = "Asia/Taipei";

        /// <summary>
        /// Gets or sets the culture applied when a user has no <c>st_user.culture</c> of their own.
        /// An empty value falls through to the language service's own default.
        /// </summary>
        /// <remarks>
        /// The default is <c>zh-TW</c> for backward compatibility: <see cref="Identity.SessionInfo.Culture"/>
        /// used to be hard-coded to that value, so every existing deployment is implicitly running on
        /// it. Deployments serving another language should set this explicitly.
        /// </remarks>
        [Category("Localization")]
        [Description("Culture applied when a user has no culture of their own (e.g. zh-TW).")]
        [DefaultValue("zh-TW")]
        public string DefaultLanguage { get; set; } = "zh-TW";

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
