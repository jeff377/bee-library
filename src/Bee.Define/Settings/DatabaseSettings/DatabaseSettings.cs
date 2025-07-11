using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料庫設定。
    /// </summary>
    [Serializable]
    [XmlType("DatabaseSettings")]
    [Description("資料庫設定。")]
    [TreeNode("Database Settings")]
    public class DatabaseSettings : IObjectSerializeFile, IObjectSerializeProcess
    {
        private string _ObjectFilePath = string.Empty;
        private SerializeState _SerializeState = SerializeState.None;
        private DateTime _CreateTime = DateTime.MinValue;
        private DatabaseItemCollection _Items = null;

        #region 建構函式

        /// <summary>
        /// 建構函式 
        /// </summary>
        public DatabaseSettings()
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
            BaseFunc.SetSerializeState(_Items, serializeState);
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

        #region IObjectSerializeProcess 介面

        /// <summary>
        /// 執行序列化前的通知方法。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        public void BeforeSerialize(SerializeFormat serializeFormat)
        {
            // 執行加密
            foreach (DatabaseItem item in this.Items)
            {
                if (StrFunc.IsNotEmpty(item.Password))
                    item.EncryptedData = CryptoFunc.AesEncrypt(item.Password);
            }
        }

        /// <summary>
        /// 執行序列化後的通知方法。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        public void AfterSerialize(SerializeFormat serializeFormat)
        {
        }

        /// <summary>
        /// 執行反序列化後的通知方法。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        public void AfterDeserialize(SerializeFormat serializeFormat)
        {
            // 執行解密
            foreach (DatabaseItem item in this.Items)
            {
                if (StrFunc.IsNotEmpty(item.EncryptedData))
                    item.Password = CryptoFunc.AesTryDecrypt(item.EncryptedData);
            }
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
        /// 資料庫連線設定集合。
        /// </summary>
        [Description("資料庫連線設定集合。")]
        [DefaultValue(null)]
        public DatabaseItemCollection Items
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Items)) { return null; }
                if (_Items == null) { _Items = new DatabaseItemCollection(); }
                return _Items;
            }
        }
    }
}
