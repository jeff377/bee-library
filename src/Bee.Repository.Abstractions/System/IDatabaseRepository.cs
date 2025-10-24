using Bee.Define;

namespace Bee.Repository.Abstractions
{
    /// <summary>
    /// 資料庫操作的抽象介面。
    /// </summary>
    public interface IDatabaseRepository
    {
        /// <summary>
        /// 測試資料庫連線，失敗時丟出例外。
        /// </summary>
        /// <param name="item">資料庫設定項。</param>
        void TestConnection(DatabaseItem item);

        /// <summary>
        /// 升級資料表結構。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        /// <remarks>回傳是否已升級。</remarks>
        bool UpgradeTableSchema(string databaseId, string dbName, string tableName);
    }
}
