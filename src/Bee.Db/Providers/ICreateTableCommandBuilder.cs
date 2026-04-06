using Bee.Define.Database;
using Bee.Define;

namespace Bee.Db.Providers
{
    /// <summary>
    /// 建立資料表命令語法產生器介面。
    /// </summary>
    public interface ICreateTableCommandBuilder
    {
        /// <summary>
        /// 取得 Create Table 的 SQL 語法。
        /// </summary>
        /// <param name="tableSchema">資料表結構。</param>
        string GetCommandText(TableSchema tableSchema);
    }
}
