using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料庫伺服器。
    /// </summary>
    [Serializable]
    [XmlType("DatabaseServer")]
    [Description("資料庫伺服器。")]
    [TreeNode]
    public class DatabaseServer : KeyCollectionItem
    {
        private string _DisplayName = string.Empty;

        /// <summary>
        /// 伺服器編號。
        /// </summary>
        [XmlAttribute]
        [Description("伺服器編號。")]
        public string ID
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
            get { return _DisplayName; }
            set { _DisplayName = value; }
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{this.ID} - {this.DisplayName}";
        }
    }
}
