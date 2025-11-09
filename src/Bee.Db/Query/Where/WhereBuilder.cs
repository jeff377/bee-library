using Bee.Define;
using System;

namespace Bee.Db
{
    /// <summary>
    /// WHERE 子句建置器。
    /// </summary>
    public sealed class WhereBuilder : IWhereBuilder
    {
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        public WhereBuilder(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        /// <summary>
        /// 由結構化條件節點建置 WHERE 子句。
        /// </summary>
        /// <param name="root">條件根節點（可為群組或單一條件）。</param>
        /// <param name="selectContext">表示 SQL 查詢所需的欄位來源與資料表 Join 關係集合。</param>
        /// <param name="includeWhereKeyword">是否在結果前加入 "WHERE "。</param>
        public WhereBuildResult Build(FilterNode root, SelectContext selectContext = null, bool includeWhereKeyword = true)
        {
            if (root == null) { return new WhereBuildResult(); }

            var filter = (selectContext != null)
                               ? RemapFilterNodeFields(root, selectContext)
                               : root;
            var parameters = new DefaultParameterCollector("@");
            var core = InternalWhereBuilder.BuildNode(filter, parameters);
            var sql = includeWhereKeyword && !string.IsNullOrEmpty(core) ? "WHERE " + core : core;

            return new WhereBuildResult
            {
                WhereClause = sql,
                Parameters = parameters.GetAll()
            };
        }


        /// <summary>
        /// 重新映射過濾節點中的欄位名稱為 SQL 查詢所需的格式（加上資料表別名）。
        /// </summary>
        /// <param name="node">要重新映射的過濾節點。</param>
        /// <param name="selectContext">表示 SQL 查詢所需的欄位來源與資料表 Join 關係集合。</param>
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

        private string QuoteIdentifier(string identifier)
        {
            return DbFunc.QuoteIdentifier(_databaseType, identifier);
        }
    }
}
