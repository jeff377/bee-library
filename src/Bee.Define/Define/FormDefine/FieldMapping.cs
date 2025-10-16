using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 欄位對應。
    /// </summary>
    [Serializable]
    [XmlType("FieldMapping")]
    [Description("欄位對應。")]
    public class FieldMapping : CollectionItem
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public FieldMapping()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="sourceField">來源欄位。</param>
        /// <param name="destinationField">目的欄位。</param>
        public FieldMapping(string sourceField, string destinationField)
        {
            SourceField = sourceField;
            DestinationField = destinationField;
        }

        #endregion

        /// <summary>
        /// 來源欄位。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("來源欄位。")]
        public string SourceField { get; set; } = string.Empty;

        /// <summary>
        /// 目的欄位。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("目的欄位。")]
        public string DestinationField { get; set; } = string.Empty;

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{SourceField} -> {DestinationField}";
        }
    }
}
