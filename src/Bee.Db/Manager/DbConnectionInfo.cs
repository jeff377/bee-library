using Bee.Define;
using System.Data.Common;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫連線資訊。
    /// </summary>
    public class DbConnectionInfo
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        /// <param name="provider">資料庫提供者。</param>
        /// <param name="connectionString">連線字串。</param>
        internal DbConnectionInfo(DatabaseType databaseType, DbProviderFactory provider, string connectionString)
        {
            DatabaseType = databaseType;
            Provider = provider;
            ConnectionString = connectionString;
        }

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        public DatabaseType DatabaseType { get; }

        /// <summary>
        /// 資料庫提供者。
        /// </summary>
        public DbProviderFactory Provider { get; }

        /// <summary>
        /// 資料庫連線字串。
        /// </summary>
        public string ConnectionString { get; }
    }
}
