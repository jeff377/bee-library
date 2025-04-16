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
    public class TDbTableIndex : TKeyCollectionItem
    {
        private bool _Unique = false;
        private bool _PrimaryKey = false;
        private TIndexFieldCollection _IndexFields = null;
        private EDbUpgradeAction _UpgradeAction = EDbUpgradeAction.None;

        /// <summary>
        /// 索引名稱。
        /// </summary>
        [XmlAttribute]
        [Category(Category.Data)]
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
        [Category(Category.Data)]
        [Description("是否具有唯一性。")]
        [DefaultValue(false)]
        public bool Unique
        {
            get { return _Unique; }
            set { _Unique = value; }
        }

        /// <summary>
        /// 是否為主鍵。
        /// </summary>
        [XmlAttribute]
        [Category(Category.Data)]
        [Description("是否為主鍵。")]
        [DefaultValue(false)]
        public bool PrimaryKey
        {
            get { return _PrimaryKey; }
            set { _PrimaryKey = value; }
        }

        /// <summary>
        /// 索引欄位集合。
        /// </summary>
        [Description("索引欄位集合。")]
        [Browsable(false)]
        [DefaultValue(null)]
        public TIndexFieldCollection IndexFields
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _IndexFields)) { return null; }
                if (_IndexFields == null) { _IndexFields = new TIndexFieldCollection(); }
                return _IndexFields;
            }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public override void SetSerializeState(ESerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_IndexFields, serializeState);
        }

        /// <summary>
        /// 索引結構升級動作。
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        [DefaultValue(EDbUpgradeAction.None)]
        public EDbUpgradeAction UpgradeAction
        {
            get { return _UpgradeAction; }
            set { _UpgradeAction = value; }
        }

        /// <summary>
        /// 建立複本。
        /// </summary>
        public TDbTableIndex Clone()
        {
            TDbTableIndex oIndex;

            oIndex = new TDbTableIndex();
            oIndex.Name = this.Name;
            oIndex.PrimaryKey = this.PrimaryKey;
            oIndex.Unique = this.Unique;
            foreach (TIndexField indexField in this.IndexFields)
                oIndex.IndexFields.Add(indexField.Clone());
            return oIndex;
        }

        /// <summary>
        /// 比較結構是否相同。
        /// </summary>
        /// <param name="source">來源物件。</param>
        public bool Compare(TDbTableIndex source)
        {
            // 唯一性不同，傳回 false
            if (this.Unique != source.Unique) { return false; }
            // 索引欄位數不同，傳回 falase
            if (this.IndexFields.Count != source.IndexFields.Count) { return false; }
            // 比對每個索引欄位結構
            foreach (TIndexField indexField in this.IndexFields)
            {
                // 索引欄位不存在，傳回 false
                if (!source.IndexFields.Contains(indexField.FieldName)) { return false; }
                // 排序方式不同，傳回 false
                if (BackendInfo.DatabaseType == EDatabaseType.SQLServer)
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
