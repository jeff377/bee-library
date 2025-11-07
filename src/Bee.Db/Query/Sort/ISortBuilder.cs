using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 定義用於產生 SQL 語法 ORDER BY 子句的介面。
    /// </summary>
    public interface ISortBuilder
    {
        /// <summary>
        /// 根據指定的排序欄位集合，產生 SQL 的 ORDER BY 子句（包含前綴關鍵字）。
        /// </summary>
        /// <param name="sortFields">排序欄位集合。</param>
        string Build(SortFieldCollection sortFields);
    }
}
