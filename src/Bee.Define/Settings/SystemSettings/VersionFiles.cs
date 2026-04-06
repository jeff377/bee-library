using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Define.Settings
{
    /// <summary>
    /// Version number and related files.
    /// </summary>
    [Serializable]
    [XmlType("VersionFiles")]
    [Description("Version number and related files.")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class VersionFiles
    {
        /// <summary>
        /// Gets or sets the version number.
        /// </summary>
        [XmlAttribute]
        [Description("Version number.")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the file list.
        /// </summary>
        [XmlAttribute]
        [Description("File list.")]
        public string Files { get; set; }
    }
}
