using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 表單資料表。
    /// </summary>
    [Serializable]
    [XmlType("FormTable")]
    [Description("表單資料表。")]
    [TreeNode]
    public class FormTable : KeyCollectionItem
    {
        private FormFieldCollection _fields = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public FormTable()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        /// <param name="displayName">顯示名稱。</param>
        public FormTable(string tableName, string displayName)
        {
            TableName = tableName;
            DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// 資料表名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("資料表名稱。")]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// 資料庫的資料表名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("資料庫的資料表名稱。")]
        public string DbTableName { get; set; } = string.Empty;

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("顯示名稱。")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 欄位集合。
        /// </summary>
        [Description("欄位集合。")]
        [DefaultValue(null)]
        public FormFieldCollection Fields
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(SerializeState, _fields)) { return null; }
                if (_fields == null) { _fields = new FormFieldCollection(this); }
                return _fields;
            }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_fields, serializeState);
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{TableName} - {DisplayName}";
        }
    }
}
