using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 定義用於產生 SQL 語法 SELECT 子句的介面。
    /// </summary>
    public interface ISelectBuilder
    {
        /// <summary>
        /// 建立 SELECT 子句。
        /// </summary>
        /// <param name="formTable">表單資料表。</param>
        /// <param name="selectFieldNames">要選取的欄位名稱集合。</param>
        /// <param name="selectContext">查詢欄位來源與 Join 關係集合。</param>
        string Build(FormTable formTable, StringHashSet selectFieldNames, SelectContext selectContext);
    }
}
