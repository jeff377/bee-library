using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Core;
using Bee.Core.Attributes;
using Bee.Core.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A database configuration item.
    /// </summary>
    [Serializable]
    [XmlType("DatabaseItem")]
    [Description("Database item.")]
    [TreeNode]
    public class DatabaseItem : KeyCollectionItem
    {
        /// <summary>
        /// Gets or sets the database ID.
        /// </summary>
        [XmlAttribute]
        [Description("Database ID.")]
        public string Id
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [XmlAttribute]
        [Description("Display name.")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the database type.
        /// </summary>
        [XmlAttribute]
        [Description("Database type.")]
        public DatabaseType DatabaseType { get; set; } = DatabaseType.SQLServer;

        /// <summary>
        /// Gets or sets the database server ID. When set, the connection string from the corresponding server is used.
        /// </summary>
        [XmlAttribute]
        [Description("Database server ID. When set, the connection string from the corresponding server is used.")]
        [DefaultValue("")]
        public string ServerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the database connection string.
        /// </summary>
        [XmlAttribute]
        [Description("Database connection string.")]
        [DefaultValue("")]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the database name, which replaces the {@DbName} placeholder in the connection string.
        /// </summary>
        [XmlAttribute]
        [Description("Database name, which replaces the {@DbName} placeholder in the connection string.")]
        [DefaultValue("")]
        public string DbName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the login user ID, which replaces the {@UserId} placeholder in the connection string.
        /// </summary>
        [XmlAttribute]
        [Description("Login user ID, which replaces the {@UserId} placeholder in the connection string.")]
        [DefaultValue("")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the login password, which replaces the {@Password} placeholder in the connection string.
        /// </summary>
        [XmlAttribute]
        [Description("Login password, which replaces the {@Password} placeholder in the connection string.")]
        [PasswordPropertyText(true)]
        [DefaultValue("")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Creates a copy of this instance.
        /// </summary>
        public DatabaseItem Clone()
        {
            return new DatabaseItem
            {
                Id = Id,
                DisplayName = DisplayName,
                DatabaseType = DatabaseType,
                ServerId = ServerId,
                ConnectionString = ConnectionString,
                DbName = DbName,
                UserId = UserId,
                Password = Password
            };
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{DbName} - {DisplayName}";
        }
    }
}
