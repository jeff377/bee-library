using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Define
{
    /// <summary>
    /// 版號及相關檔案。
    /// </summary>
    [Serializable]
    [XmlType("VersionFiles")]
    [Description("版號及相關檔案。")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class VersionFiles
    {
        /// <summary>
        /// 版號。
        /// </summary>
        [XmlAttribute]
        [Description("版號。")]
        public string Version { get; set; }

        /// <summary>
        /// 檔案清單。
        /// </summary>
        [XmlAttribute]
        [Description("檔案清單。")]
        public string Files { get; set; }
    }
}
