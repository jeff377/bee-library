using System.Data.Common;

namespace Bee.Db
{
    /// <summary>
    /// 建立表單相關命令語法產生器，包含 Select、Insert、Update、Delete 語法。
    /// </summary>
    public interface IFormCommandBuilder
    {
        /// <summary>
        /// 建立 Select 語法的資料庫命令。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        /// <param name="selectFields">要取得的欄位集合字串，以逗點分隔欄位名稱，空字串表示取得所有欄位。</param>
        DbCommand BuildSelectCommand(string tableName, string selectFields);

        /// <summary>
        /// 建立 Insert 語法的資料庫命令。
        /// </summary>
        DbCommand BuildInsertCommand();

        /// <summary>
        /// 建立 Update 語法的資料庫命令。
        /// </summary>
        DbCommand BuildUpdateCommand();

        /// <summary>
        /// 建立 Delete 語法的資料庫命令。
        /// </summary>
        DbCommand BuildDeleteCommand();
    }
}
