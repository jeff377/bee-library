using System;
using System.ComponentModel;
using System.Xml.Serialization;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 自訂屬性。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    [XmlType("Property")]
    [Description("自訂屬性。")]
    public class Property : MessagePackKeyCollectionItem
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public Property()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="name">屬性名稱。</param>
        /// <param name="value">屬性值。</param>
        public Property(string name, string value)
        {
            Name = name;
            Value = value;
        }

        #endregion

        /// <summary>
        /// 屬性名稱。
        /// </summary>
        [XmlAttribute]
        [Key(100)]
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
        [Key(101)]
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
