using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 定義用於產生 SQL 語法 WHERE 子句的介面。
    /// </summary>
    public interface IWhereBuilder
    {
        /// <summary>
        /// 由結構化條件節點建置 WHERE 子句。
        /// </summary>
        /// <param name="root">條件根節點（可為群組或單一條件）。</param>
        /// <param name="selectContext">表示 SQL 查詢所需的欄位來源與資料表 Join 關係集合。</param>
        /// <param name="includeWhereKeyword">是否在結果前加入 "WHERE "。</param>
        WhereBuildResult Build(FilterNode root, SelectContext selectContext, bool includeWhereKeyword = true);
    }
}
