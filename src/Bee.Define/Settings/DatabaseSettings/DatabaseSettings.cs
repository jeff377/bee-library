using System;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Define
{
    /// <summary>
    /// 資料庫設定。
    /// </summary>
    [Serializable]
    [XmlType("DatabaseSettings")]
    [Description("資料庫設定。")]
    [TreeNode("Database Settings")]
    public class DatabaseSettings : IObjectSerializeFile, IObjectSerializeProcess, ISerializableClone
    {
        private DatabaseServerCollection _servers = null;
        private DatabaseItemCollection _items = null;

        #region 建構函式

        /// <summary>
        /// 建構函式 
        /// </summary>
        public DatabaseSettings()
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
            BaseFunc.SetSerializeState(_servers, serializeState);
            BaseFunc.SetSerializeState(_items, serializeState);
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

        #region IObjectSerializeProcess 介面

        /// <summary>
        /// 執行序列化前的通知方法。
        /// </summary>
        /// <param name="serializeFormat">序列化格式。</param>
        public void BeforeSerialize(SerializeFormat serializeFormat)
        {
            var combinedKey = BackendInfo.ConfigEncryptionKey;
            if (combinedKey == null || combinedKey.Length == 0) return;

            AesCbcHmacKeyGenerator.FromCombinedKey(combinedKey, out var aesKey, out var hmacKey);

            foreach (var server in Servers)
            {
                if (StrFunc.IsNotEmpty(server.Password) && !server.Password.StartsWith("enc:"))
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(server.Password);
                    byte[] encrypted = AesCbcHmacCryptor.Encrypt(plainBytes, aesKey, hmacKey);
                    server.Password = "enc:" + Convert.ToBase64String(encrypted);
                }
            }

            foreach (var item in Items)
            {
                if (StrFunc.IsNotEmpty(item.Password) && !item.Password.StartsWith("enc:"))
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(item.Password);
                    byte[] encrypted = AesCbcHmacCryptor.Encrypt(plainBytes, aesKey, hmacKey);
                    item.Password = "enc:" + Convert.ToBase64String(encrypted);
                }
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
            var combinedKey = BackendInfo.ConfigEncryptionKey;
            if (combinedKey == null || combinedKey.Length == 0) return;

            AesCbcHmacKeyGenerator.FromCombinedKey(combinedKey, out var aesKey, out var hmacKey);

            foreach (var server in Servers)
            {
                if (StrFunc.IsNotEmpty(server.Password) && server.Password.StartsWith("enc:"))
                {
                    try
                    {
                        string base64 = server.Password.Substring(4);
                        byte[] encrypted = Convert.FromBase64String(base64);
                        byte[] plain = AesCbcHmacCryptor.Decrypt(encrypted, aesKey, hmacKey);
                        server.Password = Encoding.UTF8.GetString(plain);
                    }
                    catch
                    {
                        server.Password = string.Empty; // 解密失敗時保護資料
                    }
                }
            }

            foreach (var item in Items)
            {
                if (StrFunc.IsNotEmpty(item.Password) && item.Password.StartsWith("enc:"))
                {
                    try
                    {
                        string base64 = item.Password.Substring(4);
                        byte[] encrypted = Convert.FromBase64String(base64);
                        byte[] plain = AesCbcHmacCryptor.Decrypt(encrypted, aesKey, hmacKey);
                        item.Password = Encoding.UTF8.GetString(plain);
                    }
                    catch
                    {
                        item.Password = string.Empty; // 解密失敗時保護資料
                    }
                }
            }
        }

        #endregion

        #region ISerializableClone 介面   

        /// <summary>
        /// 複製出一份序列化用的物件 (深拷貝)。
        /// </summary>
        public object CreateSerializableCopy()
        {
            return Clone();
        }

        #endregion

        /// <summary>
        /// 物件建立時間。
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// 資料庫伺服器集合。
        /// </summary>
        [Description("資料庫伺服器集合。")]
        [DefaultValue(null)]
        public DatabaseServerCollection Servers
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(SerializeState, _servers)) { return null; }
                if (_servers == null) { _servers = new DatabaseServerCollection(); }
                return _servers;
            }
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
                if (BaseFunc.IsSerializeEmpty(SerializeState, _items)) { return null; }
                if (_items == null) { _items = new DatabaseItemCollection(); }
                return _items;
            }
        }

        /// <summary>
        /// 建立此物件的複本。
        /// </summary>
        public DatabaseSettings Clone()
        {
            var copy = new DatabaseSettings();
            
            foreach (var server in Servers)
                copy.Servers.Add(server.Clone());

            foreach (var item in Items)
                copy.Items.Add(item.Clone());

            return copy;
        }


    }
}
