using Bee.Define;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bee.Db
{
    /// <summary>
    /// SQL Server 資料庫建立 Select 命令產生的類別。
    /// </summary>
    internal class SqlSelectCommandBuilder
    {
        private readonly FormDefine _formDefine;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        public SqlSelectCommandBuilder(FormDefine formDefine)
        {
            _formDefine = formDefine;
        }

        /// <summary>
        /// 建立 Select 語法的 DbCommandSpec。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        /// <param name="selectFields">要取得的欄位集合字串，以逗點分隔欄位名稱，空字串表示取得所有欄位。</param>
        public DbCommandSpec Build(string tableName, string selectFields)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("tableName 不可為空", nameof(tableName));

            // 取得 FormTable
            var table = _formDefine.Tables[tableName];
            if (table == null)
                throw new InvalidOperationException($"找不到指定的資料表: {tableName}");

            var dbTableName = !string.IsNullOrWhiteSpace(table.DbTableName) ? table.DbTableName : table.TableName;

            string fields;
            if (string.IsNullOrWhiteSpace(selectFields))
            {
                // 取得所有欄位
                fields = string.Join(", ", table.Fields.Select(f => $"[{f.FieldName}]"));
            }
            else
            {
                // 只取指定欄位
                var fieldNames = selectFields.Split(',')
                    .Select(f => f.Trim())
                    .Where(f => !string.IsNullOrEmpty(f))
                    .ToArray();
                fields = string.Join(", ", fieldNames.Select(f => $"[{f}]"));
            }

            var sql = $"SELECT {fields} FROM [{dbTableName}]";
            return new DbCommandSpec(DbCommandKind.DataTable, sql, new Dictionary<string, object>());
        }
    }
}