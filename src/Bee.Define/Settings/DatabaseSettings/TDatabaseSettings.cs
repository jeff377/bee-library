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
    [TreeNode("資料庫設定")]
    public class TDatabaseSettings : IObjectSerializeFile, IObjectSerializeProcess
    {
        private string _ObjectFilePath = string.Empty;
        private ESerializeState _SerializeState = ESerializeState.None;
        private DateTime _CreateTime = DateTime.MinValue;
        private TDatabaseItemCollection _Items = null;

        #region 建構函式

        /// <summary>
        /// 建構函式 
        /// </summary>
        public TDatabaseSettings()
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
        public ESerializeState SerializeState
        {
            get { return _SerializeState; }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public void SetSerializeState(ESerializeState serializeState)
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
        public void BeforeSerialize(ESerializeFormat serializeFormat)
        {
            // 執行加密
            foreach (TDatabaseItem item in this.Items)
            {
                if (StrFunc.IsNotEmpty(item.Password))
                    item.EncryptedData = EncryptionFunc.AesEncrypt(item.Password);
            }
        }

        /// <summary>
        /// 執行序列化後的通知方法。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        public void AfterSerialize(ESerializeFormat serializeFormat)
        {
        }

        /// <summary>
        /// 執行反序列化後的通知方法。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        public void AfterDeserialize(ESerializeFormat serializeFormat)
        {
            // 執行解密
            foreach (TDatabaseItem item in this.Items)
            {
                if (StrFunc.IsNotEmpty(item.EncryptedData))
                    item.Password = EncryptionFunc.AesTryDecrypt(item.EncryptedData);
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
        public TDatabaseItemCollection Items
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Items)) { return null; }
                if (_Items == null) { _Items = new TDatabaseItemCollection(); }
                return _Items;
            }
        }
    }
}
