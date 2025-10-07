using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料表索引。
    /// </summary>
    [Serializable]
    [XmlType("DbTableIndex")]
    [Description("資料表索引。")]
    [TreeNode]
    public class DbTableIndex : KeyCollectionItem
    {
        private IndexFieldCollection _indexFields = null;

        /// <summary>
        /// 索引名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("索引名稱。")]
        public string Name
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// 是否具有唯一性。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("是否具有唯一性。")]
        [DefaultValue(false)]
        public bool Unique { get; set; } = false;

        /// <summary>
        /// 是否為主鍵。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("是否為主鍵。")]
        [DefaultValue(false)]
        public bool PrimaryKey { get; set; } = false;

        /// <summary>
        /// 索引欄位集合。
        /// </summary>
        [Description("索引欄位集合。")]
        [Browsable(false)]
        [DefaultValue(null)]
        public IndexFieldCollection IndexFields
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _indexFields)) { return null; }
                if (_indexFields == null) { _indexFields = new IndexFieldCollection(); }
                return _indexFields;
            }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_indexFields, serializeState);
        }

        /// <summary>
        /// 索引結構升級動作。
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        [DefaultValue(DbUpgradeAction.None)]
        public DbUpgradeAction UpgradeAction { get; set; } = DbUpgradeAction.None;

        /// <summary>
        /// 建立複本。
        /// </summary>
        public DbTableIndex Clone()
        {
            var index = new DbTableIndex();
            index.Name = this.Name;
            index.PrimaryKey = this.PrimaryKey;
            index.Unique = this.Unique;
            foreach (IndexField indexField in this.IndexFields)
                index.IndexFields.Add(indexField.Clone());
            return index;
        }

        /// <summary>
        /// 比較結構是否相同。
        /// </summary>
        /// <param name="source">來源物件。</param>
        public bool Compare(DbTableIndex source)
        {
            // 唯一性不同，傳回 false
            if (this.Unique != source.Unique) { return false; }
            // 索引欄位數不同，傳回 falase
            if (this.IndexFields.Count != source.IndexFields.Count) { return false; }
            // 比對每個索引欄位結構
            foreach (IndexField indexField in this.IndexFields)
            {
                // 索引欄位不存在，傳回 false
                if (!source.IndexFields.Contains(indexField.FieldName)) { return false; }
                // 排序方式不同，傳回 false
                if (BackendInfo.DatabaseType == DatabaseType.SQLServer)
                {
                    if (indexField.SortDirection != source.IndexFields[indexField.FieldName].SortDirection) { return false; }
                }
            }
            return true;
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
