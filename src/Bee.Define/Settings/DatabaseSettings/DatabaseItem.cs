﻿using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料庫項目。
    /// </summary>
    [Serializable]
    [XmlType("DatabaseItem")]
    [Description("資料庫項目。")]
    [TreeNode]
    public class DatabaseItem : KeyCollectionItem
    {
        /// <summary>
        /// 資料庫編號。
        /// </summary>
        [XmlAttribute]
        [Description("資料庫編號。")]
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
        public string DisplayName { get; set; }

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
        /// 資料庫名稱，取代連線字串中的 {@DbName} 參數。
        /// </summary>
        [XmlAttribute]
        [Description("資料庫名稱，取代連線字串中的 {@DbName} 參數。")]
        [DefaultValue("")]
        public string DbName { get; set; } = string.Empty;

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
        [Description("登入密碼，取代連線字串中的 {@Password} 參數。")]
        [PasswordPropertyText(true)]
        [DefaultValue("")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 取得資料庫連線字串。
        /// </summary>
        public string GetConnectionString()
        {
            string connectionString = this.ConnectionString;
            if (StrFunc.IsNotEmpty(this.DbName))
                connectionString = StrFunc.Replace(connectionString, "{@DbName}", this.DbName);
            if (StrFunc.IsNotEmpty(this.UserId))
                connectionString = StrFunc.Replace(connectionString, "{@UserId}", this.UserId);
            if (StrFunc.IsNotEmpty(this.Password))
                connectionString = StrFunc.Replace(connectionString, "{@Password}", this.Password);
            return connectionString;
        }

        /// <summary>
        /// 建立當前 <see cref="DatabaseItem"/> 的深拷貝。
        /// </summary>
        public DatabaseItem Clone()
        {
            return new DatabaseItem
            {
                Id = this.Id,
                DisplayName = this.DisplayName,
                DatabaseType = this.DatabaseType,
                ConnectionString = this.ConnectionString,
                DbName = this.DbName,
                UserId = this.UserId,
                Password = this.Password
            };
        }

        /// <summary>
        /// 物件描述文字。
        /// </summary>
        public override string ToString()
        {
            return $"{DbName} - {DisplayName}";
        }
    }
}
