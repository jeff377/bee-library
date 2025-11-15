using Bee.Base;
using Bee.Db;
using Bee.Define;
using Bee.Repository.Abstractions;

namespace Bee.Repository
{
    /// <summary>
    /// 資料庫操作。
    /// </summary>
    internal class DatabaseRepository : IDatabaseRepository
    {
        /// <summary>
        /// 測試資料庫連線，失敗時丟出例外。
        /// </summary>
        /// <param name="item">資料庫設定項。</param>
        public void TestConnection(DatabaseItem item)
        {
            var provider = DbProviderManager.GetFactory(item.DatabaseType);

            var connectionString = item.ConnectionString;
            if (StrFunc.IsNotEmpty(item.DbName))
                connectionString = StrFunc.Replace(connectionString, "{@DbName}", item.DbName);
            if (StrFunc.IsNotEmpty(item.UserId))
                connectionString = StrFunc.Replace(connectionString, "{@UserId}", item.UserId);
            if (StrFunc.IsNotEmpty(item.Password))
                connectionString = StrFunc.Replace(connectionString, "{@Password}", item.Password);

            using (var connection = provider.CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();
            }
        }

        /// <summary>
        /// 升級資料表結構。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        /// <remarks>回傳是否已升級。</remarks>
        public bool UpgradeTableSchema(string databaseId, string dbName, string tableName)
        {
            // 確認必要的參數不為空
            BaseFunc.EnsureNotNullOrWhiteSpace(
                (databaseId, nameof(databaseId)),
                (dbName, nameof(dbName)),
                (tableName, nameof(tableName))
            );
            var builder = new TableSchemaBuilder(databaseId);
            return builder.Execute(dbName, tableName);
        }
    }
}
