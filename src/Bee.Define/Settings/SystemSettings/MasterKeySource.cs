using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Define
{
    /// <summary>
    /// Master key source, including source type and corresponding parameter value.
    /// </summary>
    [Serializable]
    [XmlType("MasterKeySource")]
    [Description("Master key source.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class MasterKeySource
    {
        /// <summary>
        /// Master key source type.
        /// </summary>
        [Description("Master key source type.")]
        public MasterKeySourceType Type { get; set; } = MasterKeySourceType.File;

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
