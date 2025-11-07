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
        /// <param name="filter">過濾條件。</param>
        /// <param name="sortFields">排序欄位集合。</param>
        public DbCommandSpec Build(string tableName, string selectFields, FilterNode filter = null, SortFieldCollection sortFields = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("tableName cannot be null or whitespace.", nameof(tableName));

            var formTable = _formDefine.Tables[tableName];
            if (formTable == null)
                throw new InvalidOperationException($"Cannot find the specified table: {tableName}");

            var dbTableName = !string.IsNullOrWhiteSpace(formTable.DbTableName) ? formTable.DbTableName : formTable.TableName;
            var usedFieldNames = GetUsedFieldNames(formTable, selectFields, filter, sortFields);
            var selectContext = GetSelectContext(formTable, usedFieldNames);
            var selectFieldNames = GetSelectFields(formTable, selectFields);

            // 先處理 Where/Sort 欄位，讓 joins 集合完整
            FilterNode remappedFilter = null;
            if (filter != null)
            {
                remappedFilter = RemapFilterNodeFields(filter, selectContext);
            }

            SortFieldCollection remappedSortFields = null;
            if (sortFields != null && sortFields.Count > 0)
            {
                remappedSortFields = RemapSortFields(sortFields, selectContext);
            }

            var sb = new StringBuilder();
            sb.AppendLine(BuildSelectClause(formTable, selectFieldNames, selectContext));
            sb.AppendLine(BuildFromClause(dbTableName, selectContext.Joins));

            IReadOnlyDictionary<string, object> parameters = null;
            var whereClause = BuildWhereClause(remappedFilter, out parameters);
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sb.AppendLine(whereClause);
            }

            var orderByClause = BuildOrderByClause(remappedSortFields);
            if (!string.IsNullOrWhiteSpace(orderByClause))
            {
                sb.AppendLine(orderByClause);
            }

            string sql = sb.ToString();
            if (parameters != null && parameters.Count > 0)
                return new DbCommandSpec(DbCommandKind.DataTable, sql, parameters);
            else
                return new DbCommandSpec(DbCommandKind.DataTable, sql);
        }

        /// <summary>
        /// 建立 SELECT 子句。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        /// <param name="selectFieldNames">要選取的欄位名稱集合。</param>
        /// <param name="selectContext">查詢欄位來源與 Join 關係集合。</param>
        private string BuildSelectClause(FormTable formTable, StringHashSet selectFieldNames, SelectContext selectContext)
        {
            var builder = new SelectBuilder(DatabaseType.SQLServer);
            return builder.Build(formTable, selectFieldNames, selectContext);
        }

        /// <summary>
        /// 建立 FROM 子句。
        /// </summary>
        /// <param name="mainTableName">主資料表名稱。</param>
        /// <param name="joins">資料表 Join 關係集合。</param>
        /// <returns>FROM 子句字串。</returns>
        private string BuildFromClause(string mainTableName, TableJoinCollection joins)
        {
            var builder = new FromBuilder(DatabaseType.SQLServer);
            return builder.Build(mainTableName, joins);
        }

        /// <summary>
        /// 建立 WHERE 子句。
        /// </summary>
        /// <param name="remappedFilter">重新映射後的過濾條件節點。</param>
        /// <param name="parameters">回傳的參數集合。</param>
        /// <returns>WHERE 子句字串，若無條件則回傳 null。</returns>
        private string BuildWhereClause(FilterNode remappedFilter, out IReadOnlyDictionary<string, object> parameters)
        {
            parameters = null;
            if (remappedFilter != null)
            {
                var whereBuilder = new WhereBuilder();
                var whereResult = whereBuilder.Build(remappedFilter, true);
                if (!string.IsNullOrWhiteSpace(whereResult.WhereClause))
                {
                    parameters = whereResult.Parameters;
                    return whereResult.WhereClause;
                }
            }
            return null;
        }

        /// <summary>
        /// 建立 ORDER BY 子句。
        /// </summary>
        /// <param name="remappedSortFields">重新映射後的排序欄位集合。</param>
        /// <returns>ORDER BY 子句字串，若無排序則回傳 null。</returns>
        private string BuildOrderByClause(SortFieldCollection remappedSortFields)
        {
            if (remappedSortFields != null && remappedSortFields.Count > 0)
            {
                var sortBuilder = new SortBuilder();
                var orderByClause = sortBuilder.Build(remappedSortFields);
                if (!string.IsNullOrWhiteSpace(orderByClause))
                {
                    return orderByClause;
                }
            }
            return null;
        }

        /// <summary>
        /// 依據資料庫類型，回傳適當的識別字串跳脫格式。
        /// </summary>
        /// <param name="identifier">識別字名稱。</param>
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
        /// <param name="usedFieldNames">查詢使用到的欄位名稱集合。</param>
        private SelectContext GetSelectContext(FormTable formTable, HashSet<string> usedFieldNames)
        {
            var builder = new SelectContextBuilder(formTable, usedFieldNames);
            return builder.Build();
        }

        /// <summary>
        /// 重新映射過濾節點中的欄位名稱為 SQL 查詢所需的格式（加上資料表別名）。
        /// </summary>
        /// <param name="node">要重新映射的過濾節點。</param>
        /// <param name="selectContext">查詢欄位來源與 Join 關係集合。</param>
        /// <returns>重新映射後的過濾節點。</returns>
        private FilterNode RemapFilterNodeFields(FilterNode node, SelectContext selectContext)
        {
            if (node.Kind == FilterNodeKind.Condition)
            {
                var cond = (FilterCondition)node;
                var mapping = selectContext.FieldMappings.GetOrDefault(cond.FieldName);
                string fieldExpr;
                if (mapping != null)
                {
                    fieldExpr = $"{mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)}";
                }
                else
                {
                    // 本表欄位，預設別名 A
                    fieldExpr = $"A.{QuoteIdentifier(cond.FieldName)}";
                }
                return new FilterCondition(fieldExpr, cond.Operator, cond.Value);
            }
            else if (node.Kind == FilterNodeKind.Group)
            {
                var group = (FilterGroup)node;
                var newGroup = new FilterGroup(group.Operator);
                foreach (var child in group.Nodes)
                    newGroup.Nodes.Add(RemapFilterNodeFields(child, selectContext));
                return newGroup;
            }
            else
            {
                return node;
            }
        }

        /// <summary>
        /// 依據查詢欄位來源，產生 SortFIeldCollection 的複本並加上正確的 SQL 欄位表達式。
        /// </summary>
        /// <param name="sortFields">原始排序欄位集合。</param>
        /// <param name="selectContext">查詢欄位來源與 Join 關係集合。</param>
        private SortFieldCollection RemapSortFields(SortFieldCollection sortFields, SelectContext selectContext)
        {
            var result = new SortFieldCollection();
            foreach (var sortField in sortFields)
            {
                var mapping = selectContext.FieldMappings.GetOrDefault(sortField.FieldName);
                string fieldExpr;
                if (mapping != null)
                {
                    fieldExpr = $"{mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)}";
                }
                else
                {
                    fieldExpr = $"A.{QuoteIdentifier(sortField.FieldName)}";
                }
                result.Add(new SortField(fieldExpr, sortField.Direction));
            }
            return result;
        }

        /// <summary>
        /// 取得 selectFields、filter、sortFields 中使用到的不重覆的欄位名稱集合。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        /// <param name="selectFields">要取得的欄位集合字串，以逗點分隔欄位名稱，空字串表示取得所有欄位。</param>
        /// <param name="filter">過濾條件。</param>
        /// <param name="sortFields">排序欄位集合。</param>
        /// <returns>不重覆的欄位名稱集合。</returns>
        private HashSet<string> GetUsedFieldNames(FormTable formTable, string selectFields, FilterNode filter, SortFieldCollection sortFields)
        {
            var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // selectFields
            if (string.IsNullOrWhiteSpace(selectFields))
            {
                foreach (var field in formTable.Fields)
                {
                    fieldNames.Add(field.FieldName);
                }
            }
            else
            {
                var selectFieldArr = selectFields.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var field in selectFieldArr)
                {
                    fieldNames.Add(field.Trim());
                }
            }

            // filter
            CollectFilterFields(filter, fieldNames);

            // sortFields
            if (sortFields != null)
            {
                foreach (var sortField in sortFields)
                {
                    if (!string.IsNullOrWhiteSpace(sortField.FieldName))
                        fieldNames.Add(sortField.FieldName);
                }
            }

            return fieldNames;
        }

        /// <summary>
        /// 遞迴收集 FilterNode 中使用到的欄位名稱。
        /// </summary>
        /// <param name="node">過濾條件節點。</param>
        /// <param name="fieldNames">欄位名稱集合。</param>
        private void CollectFilterFields(FilterNode node, HashSet<string> fieldNames)
        {
            if (node == null) return;
            if (node.Kind == FilterNodeKind.Condition)
            {
                var cond = (FilterCondition)node;
                if (!string.IsNullOrWhiteSpace(cond.FieldName))
                    fieldNames.Add(cond.FieldName);
            }
            else if (node.Kind == FilterNodeKind.Group)
            {
                var group = (FilterGroup)node;
                foreach (var child in group.Nodes)
                    CollectFilterFields(child, fieldNames);
            }
        }
    }
}