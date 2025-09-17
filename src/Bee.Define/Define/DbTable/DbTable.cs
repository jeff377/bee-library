using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Newtonsoft.Json;

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
        private string _ObjectFilePath = string.Empty;
        private SerializeState _SerializeState = SerializeState.None;
        private DateTime _CreateTime = DateTime.MinValue;
        private string _DbName = string.Empty;
        private string _TableName = string.Empty;
        private string _DisplayName = string.Empty;
        private DbFieldCollection _Fields = null;
        private DbTableIndexCollection _Indexes = null;
        private DbUpgradeAction _UpgradeAction = DbUpgradeAction.None;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public DbTable()
        {
            _CreateTime = DateTime.Now;
        }

        #endregion

        #region IObjectSerializeFile 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState
        {
            get { return _SerializeState; }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            _SerializeState = serializeState;
            BaseFunc.SetSerializeState(_Fields, serializeState);
            BaseFunc.SetSerializeState(_Indexes, serializeState);
        }

        /// <summary>
        /// 序列化繫結檔案。
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath
        {
            get { return _ObjectFilePath; }
        }

        /// <summary>
        /// 設定序列化繫結檔案。
        /// </summary>
        /// <param name="filePath">檔案路徑。</param>
        public void SetObjectFilePath(string filePath)
        {
            _ObjectFilePath = filePath;
        }

        #endregion

        /// <summary>
        /// 物件建立時間。
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime
        {
            get { return _CreateTime; }
        }

        /// <summary>
        /// 資料庫名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("資料庫名稱。")]
        public string DbName
        {
            get { return _DbName; }
            set { _DbName = value; }
        }

        /// <summary>
        /// 資料表名稱。
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("資料表名稱。")]
        public string TableName
        {
            get { return _TableName; }
            set { _TableName = value; }
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
        [Browsable(false)]
        [DefaultValue(null)]
        public DbFieldCollection Fields
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Fields)) { return null; }
                if (_Fields == null) { _Fields = new DbFieldCollection(this); }
                return _Fields;
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
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Indexes)) { return null; }
                if (_Indexes == null) { _Indexes = new DbTableIndexCollection(this); }
                return _Indexes;
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
        public DbUpgradeAction UpgradeAction
        {
            get { return _UpgradeAction; }
            set { _UpgradeAction = value; }
        }

        /// <summary>
        /// 建立複本。
        /// </summary>
        public DbTable Clone()
        {
            DbTable oTable;

            oTable = new DbTable();
            oTable.TableName = this.TableName;
            oTable.DisplayName = this.DisplayName;
            foreach (DbTableIndex index in this.Indexes)
                oTable.Indexes.Add(index.Clone());
            foreach (DbField field in this.Fields)
                oTable.Fields.Add(field.Clone());
            return oTable;
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
