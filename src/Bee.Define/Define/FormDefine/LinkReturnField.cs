using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 關連取回欄位。
    /// </summary>
    [Serializable]
    [XmlType("LinkReturnField")]
    [Description("關連取回欄位。")]
    public class LinkReturnField : CollectionItem
    {
        private string _SourceField = string.Empty;
        private string _DestinationField = string.Empty;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public LinkReturnField()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="sourceField">來源欄位。</param>
        /// <param name="destinationField">目的欄位。</param>
        public LinkReturnField(string sourceField, string destinationField)
        {
            _SourceField = sourceField;
            _DestinationField = destinationField;
        }

        #endregion

        /// <summary>
        /// 來源欄位。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("來源欄位。")]
        public string SourceField
        {
            get { return _SourceField; }
            set { _SourceField = value; }
        }

        /// <summary>
        /// 目的欄位。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("目的欄位。")]
        public string DestinationField
        {
            get { return _DestinationField; }
            set { _DestinationField = value; }
        }
    }
}
