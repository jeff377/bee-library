using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
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
        /// 建立資料庫命令輔助類別。
        /// </summary>
        public static DbCommandHelper CreateDbCommandHelper(DatabaseType databaseType)
        {
            return new DbCommandHelper(databaseType);
        }

        /// <summary>
        /// 建立預設資料庫命令輔助類別。
        /// </summary>
        public static DbCommandHelper CreateDbCommandHelper()
        {
            return CreateDbCommandHelper(BackendInfo.DatabaseType);
        }

        /// <summary>
        /// 將 EFieldDbType 轉換為 DbType。
        /// </summary>
        /// <param name="fieldDbType">EFieldDbType 值</param>
        /// <returns>對應的 DbType</returns>
        public static DbType ConvertToDbType(FieldDbType fieldDbType)
        {
            switch (fieldDbType)
            {
                case FieldDbType.String:
                    return DbType.String;
                case FieldDbType.Text:
                    return DbType.String; // 一般的 Text 使用 String
                case FieldDbType.Boolean:
                    return DbType.Boolean;
                case FieldDbType.Identity:
                    return DbType.Int32; // Identity 在 SQL Server 中通常為 Int32
                case FieldDbType.Integer:
                    return DbType.Int32;
                case FieldDbType.Double:
                    return DbType.Double;
                case FieldDbType.Currency:
                    return DbType.Currency;
                case FieldDbType.Date:
                    return DbType.Date;
                case FieldDbType.DateTime:
                    return DbType.DateTime;
                case FieldDbType.Guid:
                    return DbType.Guid;
                case FieldDbType.Binary:
                    return DbType.Binary;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fieldDbType), $"Unsupported EFieldDbType: {fieldDbType}");
            }
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

        /// <summary>
        /// 找出 IDataReader 與 T 類別皆存在的欄位與屬性，傳回包含屬性名稱與對應欄位索引的字典。
        /// </summary>
        /// <typeparam name="T">目標類型。</typeparam>
        /// <param name="reader">資料庫查詢結果的 DbDataReader。</param>
        /// <returns>包含屬性名稱與對應欄位索引的字典。</returns>
        internal static Dictionary<string, int> GetMatchingFieldIndexes<T>(DbDataReader reader)
        {
            var fieldIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 不分大小寫比較
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);  // 取得 T 類別的所有可寫屬性名稱

            // 建立 DbDataReader 欄位名稱的 Dictionary
            var readerFields = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                readerFields[reader.GetName(i)] = i;
            }

            // 只取交集 (T 的屬性名稱 & DbDataReader 欄位名稱)
            foreach (var prop in properties)
            {
                if (prop.CanWrite && readerFields.TryGetValue(prop.Name, out int index))
                {
                    fieldIndexes[prop.Name] = index;
                }
            }

            return fieldIndexes;
        }
    }
}
