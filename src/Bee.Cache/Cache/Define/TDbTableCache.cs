using Bee.Base;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 資料表結構快取。
    /// </summary>
    internal class TDbTableCache : TKeyObjectCache<TDbTable>
    {
        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        protected override TCacheItemPolicy GetPolicy(string key)
        {
            // 拆解成員鍵值，取得資料庫名稱及資料表名稱
            StrFunc.SplitLeft(key, ".", out string dbName, out string tableName);

            // 預設為相對時間 20 分鐘
            var policy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            if (BackendInfo.DefineProvider is TFileDefineProvider)
                policy.ChangeMonitorFilePaths = new string[] { DefinePathInfo.GetDbTableFilePath(dbName, tableName) };
            return policy;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <param name="key">成員鍵值為 [資料表分類.資料表名稱]。</param>
        protected override TDbTable CreateInstance(string key)
        {
            TDbTable oValue;
            string sDbName, sTableName;

            // 拆解成員鍵值，取得資料庫名稱及資料表名稱
            StrFunc.SplitLeft(key, ".", out sDbName, out sTableName);
            oValue = BackendInfo.DefineProvider.GetDbTable(sDbName, sTableName);
            return oValue;
        }

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。。</param>
        /// <param name="tableName">資料表名稱。</param>
        public TDbTable Get(string dbName, string tableName)
        {
            string sKey;

            sKey = $"{dbName}.{tableName}";
            return base.Get(sKey);
        }

        /// <summary>
        /// 由快取區移除成員。
        /// </summary>
        /// <param name="categoryID">資料表分類。</param>
        /// <param name="tableName">資料表名稱。</param>
        public void Remove(string categoryID, string tableName)
        {
            string sKey;

            sKey = $"{categoryID}.{tableName}";
            base.Remove(sKey);  
        }
    }
}
