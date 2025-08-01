using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Define
{
    /// <summary>
    /// 資料庫結構設定。
    /// </summary>
    [Serializable]
    [XmlType("DbSchemaSettings")]
    [Description("資料庫結構設定。")]
    [TreeNode("資料庫結構")]
    public class DbSchemaSettings : IObjectSerializeFile
    {
        private DbSchemaCollection _Databases = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public DbSchemaSettings()
        {
        }

        #endregion

        #region IObjectSerializeFile 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            BaseFunc.SetSerializeState(_Databases, serializeState);
        }

        /// <summary>
        /// 序列化繫結檔案。
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
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
        /// 資料庫結構集合。
        /// </summary>
        [Description("資料庫結構集合。")]
        [DefaultValue(null)]
        public DbSchemaCollection Databases
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Databases)) { return null; }
                if (_Databases == null) { _Databases = new DbSchemaCollection(this); }
                return _Databases;
            }
        }
    }
}
