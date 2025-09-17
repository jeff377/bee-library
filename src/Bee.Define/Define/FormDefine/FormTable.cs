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
        private string _DbTableName = string.Empty;
        private string _DisplayName = string.Empty;
        private FormFieldCollection _Fields = null;

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
            this.TableName = tableName;
            _DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// 資料表名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("資料表名稱。")]
        public string TableName
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// 資料庫的資料表名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("資料庫的資料表名稱。")]
        public string DbTableName
        {
            get { return _DbTableName; }
            set { _DbTableName = value; }
        }

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("顯示名稱。")]
        public string DisplayName
        {
            get { return _DisplayName; }
            set { _DisplayName = value; }
        }

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
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Fields)) { return null; }
                if (_Fields == null) { _Fields = new FormFieldCollection(this); }
                return _Fields;
            }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_Fields, serializeState);
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
