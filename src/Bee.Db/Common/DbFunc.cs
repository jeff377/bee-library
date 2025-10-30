using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫存取函式庫。
    /// </summary>
    public static class DbFunc
    {
        /// <summary>
        /// 取得資料庫項目。
        /// </summary>
        /// <param name="databaseId">資料庫識別。</param>
        public static DatabaseItem GetDatabaseItem(string databaseId)
        {
            if (StrFunc.IsEmpty(databaseId))
                throw new ArgumentNullException(nameof(databaseId));

            var settings = BackendInfo.DefineAccess.GetDatabaseSettings();
            if (!settings.Items.Contains(databaseId))
                throw new KeyNotFoundException($"{nameof(databaseId)} '{databaseId}' not found.");

            return settings.Items[databaseId];
        }

        /// <summary>
        /// 參數名稱的前綴符號字典。
        /// </summary>
        private static readonly Dictionary<DatabaseType, string> DbParameterPrefixes = new Dictionary<DatabaseType, string>
        {
            { DatabaseType.SQLServer, "@" },
            { DatabaseType.MySQL, "@" },
            { DatabaseType.SQLite, "@" },
            { DatabaseType.Oracle, ":" }
        };

        /// <summary>
        /// 取得參數名稱的前綴符號。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        /// <returns>參數前綴符號。</returns>
        public static string GetParameterPrefix(DatabaseType databaseType)
        {
            return DbParameterPrefixes.TryGetValue(databaseType, out var prefix)
                ? prefix
                : throw new NotSupportedException($"Unsupported database type: {databaseType}.");
        }

        /// <summary>
        /// 取得含前綴符號的參數名稱。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        /// <param name="name">不含前綴符號的參數名稱。</param>
        public static string GetParameterName(DatabaseType databaseType, string name)
        {
            string parameterPrefix = GetParameterPrefix(databaseType);
            return string.IsNullOrEmpty(parameterPrefix) ? name : parameterPrefix + name;
        }

        /// <summary>
        /// 跳脫字元字典。
        /// </summary>
        private static readonly Dictionary<DatabaseType, Func<string, string>> QuoteIdentifiers = new Dictionary<DatabaseType, Func<string, string>>
        {
            { DatabaseType.SQLServer, s => $"[{s}]" },
            { DatabaseType.MySQL, s => $"`{s}`" },
            { DatabaseType.SQLite, s => $"\"{s}\"" },
            { DatabaseType.Oracle, s => $"\"{s}\"" }
        };

        /// <summary>
        /// 依據資料庫類型，回傳適當的識別字串跳脫格式。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        /// <param name="identifier">識別字名稱。</param>
        /// <returns>跳脫後的識別字。</returns>
        public static string QuoteIdentifier(DatabaseType databaseType, string identifier)
        {
            if (QuoteIdentifiers.TryGetValue(databaseType, out var func))
                return func(identifier);
            throw new NotSupportedException($"Unsupported database type: {databaseType}.");
        }

        /// <summary>
        /// 根據傳入值推斷 DbType。
        /// </summary>
        /// <param name="value">傳入值。</param>
        public static DbType? InferDbType(object value)
        {
            if (value == null || value is DBNull) return null;

            var type = value.GetType();

            if (type == typeof(string)) return DbType.String;
            if (type == typeof(int)) return DbType.Int32;
            if (type == typeof(long)) return DbType.Int64;
            if (type == typeof(short)) return DbType.Int16;
            if (type == typeof(byte)) return DbType.Byte;
            if (type == typeof(bool)) return DbType.Boolean;
            if (type == typeof(DateTime)) return DbType.DateTime;
            if (type == typeof(decimal)) return DbType.Decimal;
            if (type == typeof(double)) return DbType.Double;
            if (type == typeof(float)) return DbType.Single;
            if (type == typeof(Guid)) return DbType.Guid;
            if (type == typeof(byte[])) return DbType.Binary;
            if (type == typeof(TimeSpan)) return DbType.Time;

            // fallback：不指定，交給 Provider 自動判斷
            return null;
        }

        /// <summary>
        /// 建立資料庫連線。
        /// </summary>
        /// <param name="databaseId">資料庫識別。</param>
        public static DbConnection CreateConnection(string databaseId)
        {
            var database = DbFunc.GetDatabaseItem(databaseId);
            var provider = DbProviderManager.GetFactory(database.DatabaseType)
                    ?? throw new InvalidOperationException($"Unknown database type: {database.DatabaseType}.");
            var connection = provider.CreateConnection()
                    ?? throw new InvalidOperationException("Failed to create a database connection: DbProviderFactory.CreateConnection() returned null.");
            connection.ConnectionString = database.GetConnectionString();
            return connection;
        }

        /// <summary>
        /// SQL 命令文字格式化。
        /// </summary>
        /// <param name="s">SQL 命令文字。</param>
        /// <param name="parameters">命令參數集合。</param>
        public static string SqlFormat(string s, DbParameterCollection parameters)
        {
            string[] oArgs;

            oArgs = new string[parameters.Count];
            for (int N1 = 0; N1 < parameters.Count; N1++)
                oArgs[N1] = parameters[N1].ParameterName;
            return StrFunc.Format(s, oArgs);
        }

        /// <summary>
        /// 取得 SQL Server 資料庫的欄位預設值。
        /// </summary>
        /// <param name="dbType">欄位資料型別。</param>
        internal static string GetSqlDefaultValue(FieldDbType dbType)
        {
            switch (dbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return string.Empty;
                case FieldDbType.Boolean:
                case FieldDbType.Integer:
                case FieldDbType.Double:
                case FieldDbType.Currency:
                    return "0";
                case FieldDbType.Date:
                case FieldDbType.DateTime:
                    return "getdate()";
                case FieldDbType.Guid:
                    return "newid()";
                default:
                    return string.Empty;
            }
        }
    }
}
