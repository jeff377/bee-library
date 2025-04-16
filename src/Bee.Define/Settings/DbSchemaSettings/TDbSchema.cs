using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料庫結構。
    /// </summary>
    [Serializable]
    [XmlType("DbSchema")]
    [Description("資料庫結構。")]
    [TreeNode]
    public class TDbSchema : TKeyCollectionItem
    {
        private string _DisplayName = string.Empty;
        private TDbTableItemCollection _Tables = null;

        /// <summary>
        /// 資料庫名稱。
        /// </summary>
        [XmlAttribute]
        [Description("資料庫名稱。")]
        public string DbName
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
        /// 資料表集合。
        /// </summary>
        [Description("資料表集合。")]
        [Browsable(false)]
        [DefaultValue(null)]
        public TDbTableItemCollection Tables
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Tables)) { return null; }
                if (_Tables == null) { _Tables = new TDbTableItemCollection(this); }
                return _Tables;
            }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(ESerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_Tables, serializeState);
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{this.DbName} - {this.DisplayName}";
        }
    }
}
