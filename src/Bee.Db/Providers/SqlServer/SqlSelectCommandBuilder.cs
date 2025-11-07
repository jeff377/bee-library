using Bee.Base;
using Bee.Define;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public DbCommandSpec Build(string tableName, string selectFields, FilterNode filter = null, SortFIeldCollection sortFields = null)
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

            // 先處理 Where/Sort 欄位，讓 joins 集合完整
            FilterNode remappedFilter = null;
            if (filter != null)
            {
                remappedFilter = RemapFilterNodeFields(filter, selectContext, joins);
            }

            SortFIeldCollection remappedSortFields = null;
            if (sortFields != null && sortFields.Count > 0)
            {
                remappedSortFields = RemapSortFields(sortFields, selectContext, joins);
            }

            var sb = new StringBuilder();
            sb.AppendLine(BuildSelectClause(formTable, selectFieldNames, selectContext, joins));
            sb.AppendLine(BuildFromClause(dbTableName));
            sb.Append(BuildJoinClauses(joins));

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
        /// <param name="joins">資料表 Join 關係集合。</param>
        private string BuildSelectClause(FormTable formTable, StringHashSet selectFieldNames, SelectContext selectContext, TableJoinCollection joins)
        {
            var selectParts = new List<string>();
            foreach (var fieldName in selectFieldNames)
            {
                var field = formTable.Fields.GetOrDefault(fieldName);
                if (field == null)
                    throw new InvalidOperationException($"Field '{fieldName}' does not exist in table '{formTable.TableName}'.");
                if (field.Type == FieldType.DbField)
                {
                    selectParts.Add($"    A.{QuoteIdentifier(fieldName)}");
                }
                else
                {
                    var mapping = selectContext.FieldMappings.GetOrDefault(fieldName);
                    if (mapping == null)
                        throw new InvalidOperationException($"Field mapping for '{fieldName}' is null.");
                    selectParts.Add($"    {mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)} AS {QuoteIdentifier(fieldName)}");
                    AddTableJoin(selectContext, joins, mapping.TableJoin);
                }
            }
            return "SELECT\n" + string.Join(",\n", selectParts);
        }

        /// <summary>
        /// 建立 FROM 子句。
        /// </summary>
        /// <param name="dbTableName">資料庫的資料表名稱。</param>
        /// <returns>FROM 子句字串。</returns>
        private string BuildFromClause(string dbTableName)
        {
            return $"FROM {QuoteIdentifier(dbTableName)} A";
        }

        /// <summary>
        /// 建立 JOIN 子句。
        /// </summary>
        /// <param name="joins">資料表 Join 關係集合。</param>
        /// <returns>JOIN 子句字串。</returns>
        private string BuildJoinClauses(TableJoinCollection joins)
        {
            var sb = new StringBuilder();
            var joinList = joins.OrderBy(j => j.RightAlias);
            foreach (var join in joinList)
            {
                var joinKeyword = join.JoinType.ToString().ToUpperInvariant() + " JOIN";
                sb.AppendLine($"{joinKeyword} {QuoteIdentifier(join.RightTable)} {join.RightAlias} ON {join.LeftAlias}.{QuoteIdentifier(join.LeftField)} = {join.RightAlias}.{QuoteIdentifier(join.RightField)}");
            }
            return sb.ToString();
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
        private string BuildOrderByClause(SortFIeldCollection remappedSortFields)
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
        /// 將使用的資料表 Join 關係加入集合。
        /// </summary>
        /// <param name="context">描述 Select 查詢時所需的欄位來源與 Join 關係集合。</param>
        /// <param name="joins">資料表 Join 關係集合。</param>
        /// <param name="join">要加入資料表 Join 關係。</param>
        /// <param name="visited">已遞迴過的 Join Key 集合，用於防止環狀參照造成無窮递迴。呼叫端可不傳，預設自動建立。</param>
        private void AddTableJoin(SelectContext context, TableJoinCollection joins, TableJoin join, HashSet<string> visited = null)
        {
            if (visited == null)
                visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (visited.Contains(join.Key)) { return; }
            visited.Add(join.Key);

            if (joins.Contains(join.Key)) { return; }

            joins.Add(join);
            if (join.LeftAlias == "A") { return; }

            // 如果 LeftAlias 不為 A，表示左側非主表，要加入中間的 JOIN 關係
            var srcJoin = context.Joins.FindRightAlias(join.LeftAlias);
            if (srcJoin != null)
                AddTableJoin(context, joins, srcJoin, visited);
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
        private SelectContext GetSelectContext(FormTable formTable)
        {
            var builder = new SelectContextBuilder(formTable);
            return builder.Build();
        }

        /// <summary>
        /// 重新映射過濾節點中的欄位名稱為 SQL 查詢所需的格式（加上資料表別名）。
        /// </summary>
        /// <param name="node">要重新映射的過濾節點。</param>
        /// <param name="selectContext">查詢欄位來源與 Join 關係集合。</param>
        /// <param name="joins">使用的資料表 Join 關係。</param>
        /// <returns>重新映射後的過濾節點。</returns>
        private FilterNode RemapFilterNodeFields(FilterNode node, SelectContext selectContext, TableJoinCollection joins)
        {
            if (node.Kind == FilterNodeKind.Condition)
            {
                var cond = (FilterCondition)node;
                var mapping = selectContext.FieldMappings.GetOrDefault(cond.FieldName);
                string fieldExpr;
                if (mapping != null)
                {
                    fieldExpr = $"{mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)}";
                    AddTableJoin(selectContext, joins, mapping.TableJoin);
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
                    newGroup.Nodes.Add(RemapFilterNodeFields(child, selectContext, joins));
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
        /// <param name="joins">使用的資料表 Join 關係。</param>
        private SortFIeldCollection RemapSortFields(SortFIeldCollection sortFields, SelectContext selectContext, TableJoinCollection joins)
        {
            var result = new SortFIeldCollection();
            foreach (var sortField in sortFields)
            {
                var mapping = selectContext.FieldMappings.GetOrDefault(sortField.FieldName);
                string fieldExpr;
                if (mapping != null)
                {
                    fieldExpr = $"{mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)}";
                    AddTableJoin(selectContext, joins, mapping.TableJoin);
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
        private HashSet<string> GetUsedFieldNames(FormTable formTable, string selectFields, FilterNode filter, SortFIeldCollection sortFields)
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