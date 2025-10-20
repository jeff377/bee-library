using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 定義用於產生 SQL ORDER BY 子句的介面。
    /// </summary>
    public interface ISortBuilder
    {
        /// <summary>
        /// 根據指定的排序項目集合，產生 SQL 的 ORDER BY 子句（包含前綴關鍵字）。
        /// </summary>
        /// <param name="sorts">排序項目集合。</param>
        string Build(SortItemCollection sorts);
    }
}
