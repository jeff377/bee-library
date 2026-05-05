using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Version number and related files.
    /// </summary>
    [Description("Version number and related files.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class VersionFiles
    {
        /// <summary>
        /// Gets or sets the version number.
        /// </summary>
        [XmlAttribute]
        [Description("Version number.")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file list.
        /// </summary>
        [XmlAttribute]
        [Description("File list.")]
        public string Files { get; set; } = string.Empty;
    }
}
