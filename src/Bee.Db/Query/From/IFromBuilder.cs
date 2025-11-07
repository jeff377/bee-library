namespace Bee.Db
{
    /// <summary>
    /// 定義用於產生 SQL 語法 FROM 子句的介面。
    /// </summary>
    public interface IFromBuilder
    {
        /// <summary>
        /// 建立 FROM 子句。
        /// </summary>
        /// <param name="mainTableName">主資料表名稱。</param>
        /// <param name="joins">資料表 Join 關係集合。</param>
        /// <returns>JOIN 子句字串。</returns>
        string Build(string mainTableName, TableJoinCollection joins);
    }
}
