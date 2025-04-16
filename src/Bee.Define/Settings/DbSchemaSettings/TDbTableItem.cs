using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料表項目。
    /// </summary>
    [Serializable]
    [XmlType("DbTableItem")]
    [Description("資料表項目。")]
    [TreeNode]
    public class TDbTableItem : TKeyCollectionItem
    {
        private string _DisplayName = string.Empty;

        /// <summary>
        /// 資料表名稱。
        /// </summary>
        [XmlAttribute]
        [Description("資料表名稱。")]
        public string TableName
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlAttribute]
        [Description("顯示名稱。")]
        public string DisplayName
        {
            get { return this._DisplayName; }
            set { this._DisplayName = value; }
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{this.TableName} - {this.DisplayName}";
        }
    }
}
