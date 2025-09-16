using System;
using System.ComponentModel;
using System.Xml.Serialization;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 清單項目。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    [XmlType("ListItem")]
    public class ListItem : MessagePackKeyCollectionItem
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public ListItem()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="value">項目值。</param>
        /// <param name="text">顯示文字。</param>
        public ListItem(string value, string text)
        {
            this.Value = value;
            Text = text;
        }

        #endregion

        /// <summary>
        /// 項目值。
        /// </summary>
        [XmlAttribute]
        [Key(100)]
        [Description("項目值。")]
        public string Value
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// 顯示文字。
        /// </summary>
        [XmlAttribute]
        [Key(101)]
        [Description("顯示文字。")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 描述文字。
        /// </summary>
        public override string ToString()
        {
            return this.Text;
        }
    }
}
