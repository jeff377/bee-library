using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料庫伺服器。
    /// </summary>
    [Serializable]
    [XmlType("DatabaseServer")]
    [Description("資料庫伺服器。")]
    [TreeNode]
    public class DatabaseServer : KeyCollectionItem
    {
        /// <summary>
        /// 伺服器編號。
        /// </summary>
        [XmlAttribute]
        [Description("伺服器編號。")]
        public string Id
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlAttribute]
        [Description("顯示名稱。")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        [XmlAttribute]
        [Description("資料庫類型。")]
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;

        /// <summary>
        /// 資料庫連線字串。
        /// </summary>
        [XmlAttribute]
        [Description("資料庫連線字串。")]
        [DefaultValue("")]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// 登入用戶，取代連線字串中的 {@UserId} 參數。
        /// </summary>
        [XmlAttribute]
        [Description("登入用戶，取代連線字串中的 {@UserId} 參數。")]
        [DefaultValue("")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 登入密碼，取代連線字串中的 {@Password} 參數。
        /// </summary>
        [XmlAttribute]
        [Description("登入密碼，取代連線字串中的 {@Password} 參數。")]
        [PasswordPropertyText(true)]
        [DefaultValue("")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 建立此物件的複本。
        /// </summary>
        public DatabaseServer Clone()
        {
            return new DatabaseServer()
            {
                Id = Id,
                DisplayName = DisplayName,
                DatabaseType = DatabaseType,
                ConnectionString = ConnectionString,
                UserId = UserId,
                Password = Password
            };
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{Id} - {DisplayName}";
        }
    }
}
