using System.ComponentModel;
using Bee.Base.Attributes;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Options for the audit-trail (data-history) logging subsystem, consumed by the audit log
    /// writer registered in <c>AddBeeFramework</c>. Disabled by default — enabling it is an
    /// explicit opt-in, so existing deployments see zero behavioural change.
    /// </summary>
    [Description("Options for the audit-trail (data-history) logging subsystem.")]
    [TreeNode("AuditLog")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class AuditLogOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether audit-trail logging is enabled. Disabled by
        /// default; when disabled every consumer receives the no-op writer.
        /// </summary>
        [Category("AuditLog")]
        [Description("Whether audit-trail logging is enabled. Disabled by default (opt-in).")]
        [DefaultValue(false)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the background batch writer is used. When
        /// false, writes run synchronously — the correct choice for hosts without an
        /// <c>IHost</c> (for example an in-process local deployment) where a hosted service
        /// never starts.
        /// </summary>
        [Category("AuditLog")]
        [Description("Whether to use the background batch writer. When false, writes run synchronously (for hosts without an IHost).")]
        [DefaultValue(true)]
        public bool UseBackgroundWriter { get; set; } = true;

        /// <summary>
        /// Gets or sets the bounded in-memory queue capacity for the background writer. When the
        /// queue is full, writes degrade to synchronous so entries are never dropped.
        /// </summary>
        [Category("AuditLog")]
        [Description("Bounded in-memory queue capacity for the background writer. When full, writes degrade to synchronous.")]
        [DefaultValue(10000)]
        public int QueueCapacity { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the maximum number of entries drained and inserted per batch by the
        /// background writer.
        /// </summary>
        [Category("AuditLog")]
        [Description("Maximum number of entries inserted per batch by the background writer.")]
        [DefaultValue(100)]
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets an optional file path to spill entries to when the log database is
        /// unavailable, so they are not lost. Empty disables the file fallback.
        /// </summary>
        [Category("AuditLog")]
        [Description("Optional file path to spill entries to when the log database is unavailable. Empty disables the fallback.")]
        [DefaultValue("")]
        public string FileFallbackPath { get; set; } = string.Empty;

        /// <summary>Gets or sets a value indicating whether login records are captured.</summary>
        [Category("AuditLog")]
        [Description("Whether login records are captured.")]
        [DefaultValue(true)]
        public bool LoginEnabled { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether data-change records are captured.</summary>
        [Category("AuditLog")]
        [Description("Whether data-change records are captured.")]
        [DefaultValue(true)]
        public bool ChangeEnabled { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether execution (API call) records are captured.</summary>
        [Category("AuditLog")]
        [Description("Whether execution (API call) records are captured.")]
        [DefaultValue(true)]
        public bool ExecEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether read/access records are captured. Opt-in (off
        /// by default) because read volume is high.
        /// </summary>
        [Category("AuditLog")]
        [Description("Whether read/access records are captured. Opt-in (off by default) due to volume.")]
        [DefaultValue(false)]
        public bool AccessEnabled { get; set; }

        /// <summary>
        /// Object description.
        /// </summary>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
