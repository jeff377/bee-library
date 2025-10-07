using Bee.Base;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Define
{
    /// <summary>
    /// 資料表結構。
    /// </summary>
    [Serializable]
    [XmlType("DbTable")]
    [Description("資料表結構。")]
    [TreeNode]
    public class DbTable : IObjectSerializeFile
    {
        private DbFieldCollection _fields = null;
        private DbTableIndexCollection _indexes = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public DbTable()
        {
        }

        #endregion

        #region IObjectSerializeFile 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            BaseFunc.SetSerializeState(_fields, serializeState);
            BaseFunc.SetSerializeState(_indexes, serializeState);
        }

        /// <summary>
        /// 序列化繫結檔案。
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// 設定序列化繫結檔案。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        public void SetObjectFilePath(string filePath)
        {
            ObjectFilePath = filePath;
        }

        #endregion

        /// <summary>
        /// 物件建立時間。
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// 資料表名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("資料表名稱。")]
        public string TableName { get; set; } = string.Empty;

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
        [Browsable(false)]
        [DefaultValue(null)]
        public DbFieldCollection Fields
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _fields)) { return null; }
                if (_fields == null) { _fields = new DbFieldCollection(this); }
                return _fields;
            }
        }

        /// <summary>
        /// 索引集合。
        /// </summary>
        [Description("索引集合。")]
        [Browsable(false)]
        [DefaultValue(null)]
        public DbTableIndexCollection Indexes
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _indexes)) { return null; }
                if (_indexes == null) { _indexes = new DbTableIndexCollection(this); }
                return _indexes;
            }
        }

        /// <summary>
        /// 取得主鍵。
        /// </summary>
        public DbTableIndex GetPrimaryKey()
        {
            foreach (DbTableIndex index in this.Indexes)
            {
                if (index.PrimaryKey)
                    return index;
            }
            return null;
        }

        /// <summary>
        /// 資料表結構升級動作。
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        [DefaultValue(DbUpgradeAction.None)]
        public DbUpgradeAction UpgradeAction { get; set; } = DbUpgradeAction.None;

        /// <summary>
        /// 建立複本。
        /// </summary>
        public DbTable Clone()
        {
            var table = new DbTable();
            table.TableName = this.TableName;
            table.DisplayName = this.DisplayName;
            foreach (DbTableIndex index in this.Indexes)
                table.Indexes.Add(index.Clone());
            foreach (DbField field in this.Fields)
                table.Fields.Add(field.Clone());
            return table;
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
