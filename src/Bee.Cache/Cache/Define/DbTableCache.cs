using Bee.Base;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 資料表結構快取。
    /// </summary>
    internal class DbTableCache : KeyObjectCache<DbTable>
    {
        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        protected override CacheItemPolicy GetPolicy(string key)
        {
            // 拆解成員鍵值，取得資料庫名稱及資料表名稱
            StrFunc.SplitLeft(key, ".", out string dbName, out string tableName);

            // 預設為相對時間 20 分鐘
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineProvider is FileDefineProvider)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetDbTableFilePath(dbName, tableName) };
            return policy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <param name="key">成員鍵值為 [資料表分類.資料表名稱]。</param>
        protected override DbTable CreateInstance(string key)
        {
            // 拆解成員鍵值，取得資料庫名稱及資料表名稱
            StrFunc.SplitLeft(key, ".", out string dbName, out string tableName);
            return BackendInfo.DefineProvider.GetDbTable(dbName, tableName);
        }

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。。</param>
        /// <param name="tableName">資料表名稱。</param>
        public DbTable Get(string dbName, string tableName)
        {
            string key = $"{dbName}.{tableName}";
            return base.Get(key);
        }

        /// <summary>
        /// 由快取區移除成員。
        /// </summary>
        /// <param name="categoryID">資料表分類。</param>
        /// <param name="tableName">資料表名稱。</param>
        public void Remove(string categoryID, string tableName)
        {
            string key = $"{categoryID}.{tableName}";
            base.Remove(key);
        }
    }
}
