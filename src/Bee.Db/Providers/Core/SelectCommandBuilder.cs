using Bee.Define;
using System;
using System.Collections.Generic;

namespace Bee.Db
{
    /// <summary>
    /// 建立 Select 命令產生的類別。
    /// </summary>
    public class SelectCommandBuilder
    {
        private readonly FormDefine _formDefine;
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="formDefine">表單定義。</param>
        /// <param name="databaseType">資料庫類型。</param>
        public SelectCommandBuilder(FormDefine formDefine, DatabaseType databaseType)
        {
            _formDefine = formDefine;
            _databaseType = databaseType;
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

            var selectContext = GetSelectContext(formTable, selectFields, filter, sortFields);

            var sqlParts = new List<string>();
            sqlParts.Add(BuildSelectClause(formTable, selectFields, selectContext));
            sqlParts.Add(BuildFromClause(formTable, selectContext.Joins));

            var (whereClause, parameters) = BuildWhereClause(filter, selectContext);
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sqlParts.Add(whereClause);
            }

            var orderByClause = BuildOrderByClause(sortFields, selectContext);
            if (!string.IsNullOrWhiteSpace(orderByClause))
            {
                sqlParts.Add(orderByClause);
            }

            string sql = string.Join(Environment.NewLine, sqlParts);
            return new DbCommandSpec(DbCommandKind.DataTable, sql, parameters);
        }

        /// <summary>
        /// 取得 Select 查詢時所需的欄位來源與 Join 關係集合。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        /// <param name="selectFields">要取得的欄位集合字串，以逗點分隔欄位名稱，空字串表示取得所有欄位。</param>
        /// <param name="filter">過濾條件。</param>
        /// <param name="sortFields">排序欄位集合。</param>
        private SelectContext GetSelectContext(FormTable formTable, string selectFields, FilterNode filter, SortFieldCollection sortFields)
        {
            var usedFieldNames = GetUsedFieldNames(formTable, selectFields, filter, sortFields);
            var builder = new SelectContextBuilder(formTable, usedFieldNames);
            return builder.Build();
        }

        /// <summary>
        /// 建立 SELECT 子句。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        /// <param name="selectFields">要取得的欄位集合字串，以逗點分隔欄位名稱，空字串表示取得所有欄位。</param>
        /// <param name="selectContext">查詢欄位來源與 Join 關係集合。</param>
        private string BuildSelectClause(FormTable formTable, string selectFields, SelectContext selectContext)
        {
            var builder = new SelectBuilder(_databaseType);
            return builder.Build(formTable, selectFields, selectContext);
        }

        /// <summary>
        ///  建立 FROM 子句。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        /// <param name="joins">資料表 Join 關係集合。</param>
        /// <returns>FROM 子句字串。</returns>
        private string BuildFromClause(FormTable formTable, TableJoinCollection joins)
        {
            string mainTableName = !string.IsNullOrWhiteSpace(formTable.DbTableName) ? formTable.DbTableName : formTable.TableName;
            var builder = new FromBuilder(_databaseType);
            return builder.Build(mainTableName, joins);
        }

        /// <summary>
        /// 建立 WHERE 子句。
        /// </summary>
        /// <param name="filter">過濾條件。</param>
        /// <param name="selectContext">表示 SQL 查詢所需的欄位來源與資料表 Join 關係集合。</param>
        /// <returns>包含 WHERE 子句字串和參數集合的元組。</returns>
        private (string WhereClause, IDictionary<string, object> Parameters) BuildWhereClause(FilterNode filter, SelectContext selectContext)
        {
            var whereBuilder = new WhereBuilder(_databaseType);
            var whereResult = whereBuilder.Build(filter, selectContext, true);
            return (whereResult.WhereClause, whereResult.Parameters);
        }

        /// <summary>
        /// 建立 ORDER BY 子句。
        /// </summary>
        /// <param name="sortFields">排序欄位集合。</param>
        /// <param name="selectContext">表示 SQL 查詢所需的欄位來源與資料表 Join 關係集合。</param>
        /// <returns>ORDER BY 子句字串，若無排序則回傳 null。</returns>
        private string BuildOrderByClause(SortFieldCollection sortFields, SelectContext selectContext)
        {
            var sortBuilder = new SortBuilder(_databaseType);
            return sortBuilder.Build(sortFields, selectContext);
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