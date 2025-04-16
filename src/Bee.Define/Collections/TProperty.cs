using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 自訂屬性。
    /// </summary>
    [Serializable]
    [XmlType("Property")]
    [Description("自訂屬性。")]
    public class TProperty : TKeyCollectionItem
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TProperty()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="name">屬性名稱。</param>
        /// <param name="value">屬性值。</param>
        public TProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }

        #endregion

        /// <summary>
        /// 屬性名稱。
        /// </summary>
        [XmlAttribute]
        [Description("屬性名稱。")]
        public string Name
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// 屬性值。
        /// </summary>
        [XmlAttribute]
        [Description("屬性值。")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 物件的描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }
}
