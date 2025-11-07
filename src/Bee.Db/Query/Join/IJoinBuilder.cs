namespace Bee.Db
{
    /// <summary>
    /// 定義用於產生 SQL 語法 JOIN 子句的介面。
    /// </summary>
    public interface IJoinBuilder
    {
        /// <summary>
        /// 建立 JOIN 子句。
        /// </summary>
        /// <param name="joins">資料表 Join 關係集合。</param>
        /// <returns>JOIN 子句字串。</returns>
        string Build(TableJoinCollection joins);
    }
}
