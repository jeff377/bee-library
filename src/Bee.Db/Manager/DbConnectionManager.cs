using Bee.Base;
using Bee.Define;
using System;
using System.Collections.Concurrent;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫連線管理類別，統一管理資料庫連線資訊的快取。
    /// 與 <see cref="DbProviderManager"/> 對應，專注於連線資訊的快取管理。
    /// </summary>
    public static class DbConnectionManager
    {
        /// <summary>
        /// 靜態建構函式，在類別首次被引用時執行
        /// </summary>
        static DbConnectionManager()
        {
            // 訂閱資料庫設定變更事件
            GlobalEvents.DatabaseSettingsChanged += OnDatabaseSettingsChanged;
        }

        private static void OnDatabaseSettingsChanged(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>
        /// 連線資訊快取（執行緒安全）。
        /// </summary>
        private static readonly ConcurrentDictionary<string, DbConnectionInfo> _cache
            = new ConcurrentDictionary<string, DbConnectionInfo>();

        /// <summary>
        /// 取得或建立資料庫連線資訊（含快取）。
        /// 首次呼叫時會建立連線資訊並快取，後續呼叫直接回傳快取結果。
        /// </summary>
        /// <param name="databaseId">資料庫識別。</param>
        /// <returns>包含資料庫類型、提供者與連線字串的連線資訊物件。</returns>
        /// <exception cref="ArgumentNullException">當 <paramref name="databaseId"/> 為空或 null 時拋出。</exception>
        /// <exception cref="InvalidOperationException">當資料庫項目不存在、未註冊提供者或連線字串無效時拋出。</exception>
        public static DbConnectionInfo GetConnectionInfo(string databaseId)
        {
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentNullException(nameof(databaseId), "Database ID cannot be null or empty.");

            return _cache.GetOrAdd(databaseId, CreateConnectionInfo);
        }

        /// <summary>
        /// 建立資料庫連線資訊。
        /// </summary>
        /// <param name="databaseId">資料庫識別。</param>
        /// <returns>新建立的連線資訊物件。</returns>
        /// <exception cref="InvalidOperationException">當資料庫項目不存在、未註冊提供者或連線字串無效時拋出。</exception>
        private static DbConnectionInfo CreateConnectionInfo(string databaseId)
        {
            // 取得資料庫設定（透過 DefineAccess，內部會使用快取）
            var settings = BackendInfo.DefineAccess.GetDatabaseSettings();

            // 取得資料庫項目
            var databaseItem = settings.Items[databaseId];
            if (databaseItem == null)
                throw new InvalidOperationException($"DatabaseItem for id '{databaseId}' was not found.");

            // 預設使用 DatabaseItem 的設定
            var databaseType = databaseItem.DatabaseType;
            string connectionString = databaseItem.ConnectionString;
            string userId = databaseItem.UserId;
            string password = databaseItem.Password;
            string dbName = databaseItem.DbName;

            // 如果有設定 ServerId，從對應的 Server 取得連線字串模板
            if (StrFunc.IsNotEmpty(databaseItem.ServerId))
            {
                var server = settings.Servers[databaseItem.ServerId];
                if (server == null)
                {
                    throw new InvalidOperationException(
                        $"DatabaseServer '{databaseItem.ServerId}' referenced by DatabaseItem '{databaseId}' was not found.");
                }

                // 使用 Server 的設定作為基礎
                connectionString = server.ConnectionString;
                databaseType = server.DatabaseType;

                // DatabaseItem 可以覆蓋 Server 的 UserId/Password（如果有設定）
                if (StrFunc.IsEmpty(userId))
                    userId = server.UserId;
                if (StrFunc.IsEmpty(password))
                    password = server.Password;
            }

            // 替換連線字串中的參數
            if (StrFunc.IsNotEmpty(dbName))
                connectionString = StrFunc.Replace(connectionString, "{@DbName}", dbName);
            if (StrFunc.IsNotEmpty(userId))
                connectionString = StrFunc.Replace(connectionString, "{@UserId}", userId);
            if (StrFunc.IsNotEmpty(password))
                connectionString = StrFunc.Replace(connectionString, "{@Password}", password);

            // 驗證連線字串
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"Connection string for database '{databaseId}' is null or empty.");

            // 取得資料庫提供者
            var provider = DbProviderManager.GetFactory(databaseType)
                ?? throw new InvalidOperationException($"Unknown database type: {databaseType}.");

            return new DbConnectionInfo(databaseType, provider, connectionString);
        }

        /// <summary>
        /// 清除指定資料庫的連線資訊快取（當設定變更時使用）。
        /// </summary>
        /// <param name="databaseId">資料庫識別。</param>
        /// <returns>若成功移除快取項目則回傳 true；若項目不存在則回傳 false。</returns>
        public static bool Remove(string databaseId)
        {
            return _cache.TryRemove(databaseId, out _);
        }

        /// <summary>
        /// 清除所有連線資訊快取。
        /// </summary>
        public static void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// 檢查指定資料庫的連線資訊是否已快取。
        /// </summary>
        /// <param name="databaseId">資料庫識別。</param>
        /// <returns>若已快取則回傳 true，否則回傳 false。</returns>
        public static bool Contains(string databaseId)
        {
            return _cache.ContainsKey(databaseId);
        }

        /// <summary>
        /// 取得目前快取的資料庫連線資訊數量。
        /// </summary>
        public static int Count => _cache.Count;
    }
}
