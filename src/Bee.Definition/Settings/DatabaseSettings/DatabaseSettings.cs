using System;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Security;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using System.Text.Json.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Database settings.
    /// </summary>
    [Serializable]
    [XmlType("DatabaseSettings")]
    [Description("Database settings.")]
    [TreeNode("Database Settings")]
    public class DatabaseSettings : IObjectSerializeFile, IObjectSerializeProcess, ISerializableClone
    {
        private DatabaseServerCollection? _servers = null;
        private DatabaseItemCollection? _items = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="DatabaseSettings"/>.
        /// </summary>
        public DatabaseSettings()
        {
        }

        #endregion

        #region IObjectSerializeFile Interface

        /// <summary>
        /// Gets the serialization state.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            BaseFunc.SetSerializeState(_servers!, serializeState);
            BaseFunc.SetSerializeState(_items!, serializeState);
        }

        /// <summary>
        /// Gets the file path bound to serialization.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// Sets the file path bound to serialization.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public void SetObjectFilePath(string filePath)
        {
            ObjectFilePath = filePath;
        }

        #endregion

        #region IObjectSerializeProcess Interface

        /// <summary>
        /// Called before serialization.
        /// </summary>
        /// <param name="serializeFormat">The serialization format.</param>
        public void BeforeSerialize(SerializeFormat serializeFormat)
        {
            var combinedKey = BackendInfo.ConfigEncryptionKey;
            if (combinedKey == null || combinedKey.Length == 0) return;

            AesCbcHmacKeyGenerator.FromCombinedKey(combinedKey, out var aesKey, out var hmacKey);

            foreach (var server in Servers!)
            {
                if (StrFunc.IsNotEmpty(server.Password) && !server.Password.StartsWith("enc:"))
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(server.Password);
                    byte[] encrypted = AesCbcHmacCryptor.Encrypt(plainBytes, aesKey, hmacKey);
                    server.Password = "enc:" + Convert.ToBase64String(encrypted);
                }
            }

            foreach (var item in Items!)
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
        /// Called after serialization.
        /// </summary>
        /// <param name="serializeFormat">The serialization format.</param>
        public void AfterSerialize(SerializeFormat serializeFormat)
        {
        }

        /// <summary>
        /// Called after deserialization.
        /// </summary>
        /// <param name="serializeFormat">The serialization format.</param>
        public void AfterDeserialize(SerializeFormat serializeFormat)
        {
            var combinedKey = BackendInfo.ConfigEncryptionKey;
            if (combinedKey == null || combinedKey.Length == 0) return;

            AesCbcHmacKeyGenerator.FromCombinedKey(combinedKey, out var aesKey, out var hmacKey);

            foreach (var server in Servers!)
                server.Password = DecryptPassword(server.Password, aesKey, hmacKey);

            foreach (var item in Items!)
                item.Password = DecryptPassword(item.Password, aesKey, hmacKey);
        }

        private static string DecryptPassword(string password, byte[] aesKey, byte[] hmacKey)
        {
            if (StrFunc.IsEmpty(password) || !password.StartsWith("enc:"))
                return password;

            try
            {
                string base64 = password.Substring(4);
                byte[] encrypted = Convert.FromBase64String(base64);
                byte[] plain = AesCbcHmacCryptor.Decrypt(encrypted, aesKey, hmacKey);
                return Encoding.UTF8.GetString(plain);
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion

        #region ISerializableClone Interface

        /// <summary>
        /// Creates a serializable deep copy of this object.
        /// </summary>
        public object CreateSerializableCopy()
        {
            return Clone();
        }

        #endregion

        /// <summary>
        /// Gets the time at which this object was created.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// Gets the database server collection.
        /// </summary>
        [Description("Database server collection.")]
        [DefaultValue(null)]
        public DatabaseServerCollection? Servers
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _servers!)) { return null; }
                if (_servers == null) { _servers = new DatabaseServerCollection(); }
                return _servers;
            }
        }

        /// <summary>
        /// Gets the database connection settings collection.
        /// </summary>
        [Description("Database connection settings collection.")]
        [DefaultValue(null)]
        public DatabaseItemCollection? Items
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _items!)) { return null; }
                if (_items == null) { _items = new DatabaseItemCollection(); }
                return _items;
            }
        }

        /// <summary>
        /// Creates a copy of this instance.
        /// </summary>
        public DatabaseSettings Clone()
        {
            var copy = new DatabaseSettings();

            foreach (var server in Servers!)
                copy.Servers!.Add(server.Clone());

            foreach (var item in Items!)
                copy.Items!.Add(item.Clone());

            return copy;
        }


    }
}
