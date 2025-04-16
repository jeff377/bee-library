using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 清單項目。
    /// </summary>
    [Serializable]
    [XmlType("ListItem")]
    public class TListItem : TKeyCollectionItem
    {
        private string _Text = string.Empty;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TListItem()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="value">項目值。</param>
        /// <param name="text">顯示文字。</param>
        public TListItem(string value, string text)
        {
            this.Value = value;
            _Text = text;
        }

        #endregion

        /// <summary>
        /// 項目值。
        /// </summary>
        [XmlAttribute]
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
        [Description("顯示文字。")]
        public string Text
        {
            get { return _Text; }
            set { _Text = value; }
        }

        /// <summary>
        /// 描述文字。
        /// </summary>
        public override string ToString()
        {
            return this.Text;
        }
    }
}
