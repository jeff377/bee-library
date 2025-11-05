using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// WHERE 子句建置器介面（可因資料庫方言擴充）。
    /// </summary>
    public interface IWhereBuilder
    {
        /// <summary>
        /// 由結構化條件節點建置 WHERE 子句。
        /// </summary>
        /// <param name="root">條件根節點（可為群組或單一條件）。</param>
        /// <param name="includeWhereKeyword">是否在結果前加入 "WHERE "。</param>
        WhereBuildResult Build(FilterNode root, bool includeWhereKeyword = true);
    }
}
