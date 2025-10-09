using System.Collections.Generic;

namespace Bee.Db
{
    /// <summary>
    /// 負責依據查詢需求產生完整 SelectContext 內容的建構類別。
    /// 依據指定的查詢欄位、條件等，建立對應的 QueryFieldMapping 與 TableJoin 集合。
    /// </summary>
    public class SelectContextBuilder
    {
        /// <summary>
        /// 產生 SelectContext。
        /// </summary>
        /// <param name="selectFields">查詢欄位清單（如 Select 子句欄位）。</param>
        /// <param name="whereFields">查詢條件欄位清單（如 Where 子句欄位）。</param>
        /// <returns>完整的 SelectContext。</returns>
        public SelectContext Build(IEnumerable<string> selectFields, IEnumerable<string> whereFields)
        {
            var context = new SelectContext();

            // TODO: 根據 selectFields、whereFields 解析來源，建立 QueryFieldMapping 與 TableJoin
            // 範例：context.FieldMappings.Add(new QueryFieldMapping { ... });
            // 範例：context.Joins.Add(new TableJoin { ... });

            return context;
        }
    }
}
