using Bee.Define;
using System;

namespace Bee.Db
{
    /// <summary>
    /// WHERE 子句建置器。
    /// </summary>
    public sealed class WhereBuilder : IWhereBuilder
    {
        /// <summary>
        /// 由結構化條件節點建置 WHERE 子句。
        /// </summary>
        /// <param name="root">條件根節點（可為群組或單一條件）。</param>
        /// <param name="includeWhereKeyword">是否在結果前加入 "WHERE "。</param>
        public WhereBuildResult Build(FilterNode root, bool includeWhereKeyword = true)
        {
            if (root == null) throw new ArgumentNullException("root", "Filter root cannot be null.");

            var parameters = new DefaultParameterCollector("@");
            var core = InternalWhereBuilder.BuildNode(root, parameters);
            var sql = includeWhereKeyword && !string.IsNullOrEmpty(core) ? "WHERE " + core : core;

            return new WhereBuildResult
            {
                WhereClause = sql,
                Parameters = parameters.GetAll()
            };
        }
    }
}
