using System.ComponentModel;
using Bee.Definition.Security;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Master key source, including source type and corresponding parameter value.
    /// </summary>
    [Description("Master key source.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class MasterKeySource
    {
        /// <summary>
        /// Master key source type.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="MasterKeySourceType.Environment"/> so production hosts
        /// follow the 12-factor "config in env" principle out of the box. Container /
        /// Kubernetes / cloud-function deployments only need to inject
        /// <c>BEE_MASTER_KEY</c>; no extra volume mount or build step is required.
        /// Existing deployments with an explicit <c>&lt;Type&gt;File&lt;/Type&gt;</c>
        /// in <c>SystemSettings.xml</c> are unaffected.
        /// </remarks>
        [Description("Master key source type.")]
        public MasterKeySourceType Type { get; set; } = MasterKeySourceType.Environment;

        /// <summary>
        /// Source parameter value: file path or environment variable name.
        /// If empty, the default value will be used.
        /// </summary>
        [Description("Source parameter value, file path or environment variable name. If empty, the default value will be used.")]
        [DefaultValue("")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Convert the master key source to a string representation.
        /// </summary>
        public override string ToString()
        {
            return Type.ToString();
        }
    }

}
