using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Bee.Db
{
    /// <summary>
    /// 統一管理資料庫連線生命週期的範圍物件。若由本類別建立連線則在 Dispose() 關閉；
    /// 若使用外部傳入的連線則不關閉，僅確保可用（必要時幫忙開啟）。 
    /// </summary>
    public sealed class DbConnectionScope : IDisposable
    {
        /// <summary>
        /// 取得目前使用的資料庫連線。
        /// </summary>
        public DbConnection Connection { get; private set; }

        private readonly bool _ownsConnection;

        private DbConnectionScope(DbConnection connection, bool ownsConnection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _ownsConnection = ownsConnection;
        }

        /// <summary>
        /// 以同步方式建立連線範圍。
        /// </summary>
        /// <param name="externalConnection">外部提供的連線；若為 null 則會自行建立。</param>
        /// <param name="factory">資料庫提供者工廠。</param>
        /// <param name="connectionString">連線字串（僅在自行建立時使用）。</param>
        public static DbConnectionScope Create(DbConnection externalConnection, DbProviderFactory factory, string connectionString)
        {
            if (externalConnection != null)
            {
                EnsureOpenSync(externalConnection);
                return new DbConnectionScope(externalConnection, ownsConnection: false);
            }

            if (factory == null) throw new ArgumentNullException(nameof(factory), "Factory cannot be null.");

            var conn = factory.CreateConnection()
                       ?? throw new InvalidOperationException("Failed to create database connection: DbProviderFactory.CreateConnection() returned null.");
            conn.ConnectionString = connectionString;
            conn.Open();
            return new DbConnectionScope(conn, ownsConnection: true);
        }

        /// <summary>
        /// 以非同步方式建立連線範圍。
        /// </summary>
        /// <param name="externalConnection">外部提供的連線；若為 null 則會自行建立。</param>
        /// <param name="factory">資料庫提供者工廠。</param>
        /// <param name="connectionString">連線字串（僅在自行建立時使用）。</param>
        /// <param name="cancellationToken">取消權杖。</param>
        public static async Task<DbConnectionScope> CreateAsync(
            DbConnection externalConnection,
            DbProviderFactory factory,
            string connectionString,
            CancellationToken cancellationToken = default)
        {
            if (externalConnection != null)
            {
                await EnsureOpenAsync(externalConnection, cancellationToken).ConfigureAwait(false);
                return new DbConnectionScope(externalConnection, ownsConnection: false);
            }

            if (factory == null) throw new ArgumentNullException(nameof(factory), "Factory cannot be null.");

            var conn = factory.CreateConnection()
                       ?? throw new InvalidOperationException("Failed to create database connection: DbProviderFactory.CreateConnection() returned null.");
            conn.ConnectionString = connectionString;
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            return new DbConnectionScope(conn, ownsConnection: true);
        }

        /// <summary>
        /// 釋放資源。僅當連線為本類別建立時，才會關閉。
        /// </summary>
        public void Dispose()
        {
            if (_ownsConnection)
            {
                Connection?.Dispose();
            }
            Connection = null;
        }

        private static void EnsureOpenSync(DbConnection connection)
        {
            // Broken 視同需重開；Closed 則直接 Open
            if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken)
            {
                connection.Open();
            }
        }

        private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken ct)
        {
            if (connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken)
            {
                await connection.OpenAsync(ct).ConfigureAwait(false);
            }
        }
    }

}
