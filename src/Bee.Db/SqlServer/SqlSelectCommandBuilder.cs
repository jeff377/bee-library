using Bee.Base;
using Bee.Define;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bee.Db
{
    /// <summary>
    /// SQL Server 資料庫建立 Select 命令產生的類別。
    /// </summary>
    public class SqlSelectCommandBuilder
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
                throw new ArgumentException("tableName cannot be null or whitespace.", nameof(tableName));

            var formTable = _formDefine.Tables[tableName];
            if (formTable == null)
                throw new InvalidOperationException($"Cannot find the specified table: {tableName}");

            var dbTableName = !string.IsNullOrWhiteSpace(formTable.DbTableName) ? formTable.DbTableName : formTable.TableName;
            var selectContext = GetSelectContext(formTable);
            var selectFieldNames = GetSelectFields(formTable, selectFields);
            var joins = new TableJoinCollection();

            var sb = new StringBuilder();
            var selectParts = new List<string>();
            foreach (var fieldName in selectFieldNames)
            {
                var field = formTable.Fields.GetOrDefault(fieldName);
                if (field == null)
                    throw new InvalidOperationException($"Field '{fieldName}' does not exist in table '{formTable.TableName}'.");
                if (field.Type == FieldType.DbField)
                    selectParts.Add($"    A.{QuoteIdentifier(fieldName)}");
                else
                {
                    var mapping = selectContext.FieldMappings.GetOrDefault(fieldName);
                    if (mapping == null)
                        throw new InvalidOperationException($"Field mapping for '{fieldName}' is null.");
                    selectParts.Add($"    {mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)} AS {QuoteIdentifier(fieldName)}");
                    if (!joins.Contains(mapping.TableJoin.Key))
                        joins.Add(mapping.TableJoin);
                }
            }

            sb.AppendLine("SELECT");
            sb.AppendLine(string.Join(",\n", selectParts));
            sb.AppendLine($"FROM {QuoteIdentifier(dbTableName)} A");

            foreach (var join in joins)
            {
                var joinKeyword = join.JoinType.ToString().ToUpperInvariant() + " JOIN";
                sb.AppendLine($"{joinKeyword} {QuoteIdentifier(join.RightTable)} {join.RightAlias} ON {join.LeftAlias}.{QuoteIdentifier(join.LeftField)} = {join.RightAlias}.{QuoteIdentifier(join.RightField)}");
            }

            string sql = sb.ToString();
            return new DbCommandSpec(DbCommandKind.DataTable, sql);
        }

        /// <summary>
        /// 依據資料庫類型，回傳適當的識別字串跳脫格式。
        /// </summary>
        /// <param name="identifier">識別字名稱。</param>
        /// <returns>跳脫後的識別字。</returns>
        private string QuoteIdentifier(string identifier)
        {
            return DbFunc.QuoteIdentifier(DatabaseType.SQLServer, identifier);
        }

        /// <summary>
        /// 取得 Select 的欄位集合。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        /// <param name="selectFields">要取得的欄位集合字串，以逗點分隔欄位名稱，空字串表示取得所有欄位</param>
        private StringHashSet GetSelectFields(FormTable formTable, string selectFields)
        {
            var set = new StringHashSet();
            if (string.IsNullOrWhiteSpace(selectFields))
            {
                // 取得所有欄位
                foreach (var field in formTable.Fields)
                {
                    set.Add(field.FieldName);
                }
            }
            else
            {
                // 只取指定欄位
                set.Add(selectFields, ",");
            }
            return set;
        }

        /// <summary>
        /// 取得 Select 查詢時所需的欄位來源與 Join 關係集合。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        private SelectContext GetSelectContext(FormTable formTable)
        {
            var builder = new SelectContextBuilder(formTable);
            return builder.Build();
        }


    }
}