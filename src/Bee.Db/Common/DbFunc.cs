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
        public static IDbCommandHelper CreateDbCommandHelper(EDatabaseType databaseType)
        {
            return new TDbCommandHelper(databaseType);
        }

        /// <summary>
        /// 建立預設資料庫命令輔助類別。
        /// </summary>
        public static IDbCommandHelper CreateDbCommandHelper()
        {
            return CreateDbCommandHelper(BackendInfo.DatabaseType);
        }

        /// <summary>
        /// 將 EFieldDbType 轉換為 DbType。
        /// </summary>
        /// <param name="fieldDbType">EFieldDbType 值</param>
        /// <returns>對應的 DbType</returns>
        public static DbType ConvertToDbType(EFieldDbType fieldDbType)
        {
            switch (fieldDbType)
            {
                case EFieldDbType.String:
                    return DbType.String;
                case EFieldDbType.Text:
                    return DbType.String; // 一般的 Text 使用 String
                case EFieldDbType.Boolean:
                    return DbType.Boolean;
                case EFieldDbType.Identity:
                    return DbType.Int32; // Identity 在 SQL Server 中通常為 Int32
                case EFieldDbType.Integer:
                    return DbType.Int32;
                case EFieldDbType.Double:
                    return DbType.Double;
                case EFieldDbType.Currency:
                    return DbType.Currency;
                case EFieldDbType.Date:
                    return DbType.Date;
                case EFieldDbType.DateTime:
                    return DbType.DateTime;
                case EFieldDbType.Guid:
                    return DbType.Guid;
                case EFieldDbType.Binary:
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
        internal static string GetSqlDefaultValue(EFieldDbType dbType)
        {
            switch (dbType)
            {
                case EFieldDbType.String:
                case EFieldDbType.Text:
                    return string.Empty;
                case EFieldDbType.Boolean:
                case EFieldDbType.Integer:           
                case EFieldDbType.Double:
                case EFieldDbType.Currency:
                    return "0";
                case EFieldDbType.Date:
                case EFieldDbType.DateTime:
                    return "getdate()";
                case EFieldDbType.Guid:
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
