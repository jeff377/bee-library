using System;
using System.Runtime.Caching;
using Bee.Base;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// 快取函式庫。
    /// </summary>
    public static class CacheFunc
    {
        /// <summary>
        /// 建立快取項目的回收條件。
        /// </summary>
        /// <param name="policy">快取項目的回收條件。</param>
        internal static CacheItemPolicy CreateCachePolicy(TCacheItemPolicy policy)
        {
            var itemPolicy = new CacheItemPolicy();
            if (policy.AbsoluteExpiration != DateTimeOffset.MaxValue)
                itemPolicy.AbsoluteExpiration = policy.AbsoluteExpiration;
            if (policy.SlidingExpiration != TimeSpan.Zero)
                itemPolicy.SlidingExpiration = policy.SlidingExpiration;
            if (policy.ChangeMonitorFilePaths != null)
                itemPolicy.ChangeMonitors.Add(new HostFileChangeMonitor(policy.ChangeMonitorFilePaths));
            return itemPolicy;
        }

        /// <summary>
        /// 取得系統設定。
        /// </summary>
        public static TSystemSettings GetSystemSettings()
        {
            TSystemSettingsCache oCache;

            oCache = new TSystemSettingsCache();
            return oCache.Get();
        }

        /// <summary>
        /// 取得資料庫設定。
        /// </summary>
        public static TDatabaseSettings GetDatabaseSettings()
        {
            TDatabaseSettingsCache oCache;

            oCache = new TDatabaseSettingsCache();
            return oCache.Get();
        }

        /// <summary>
        /// 取得程式清單。
        /// </summary>
        public static TProgramSettings GetProgramSettings()
        {
            TProgramSettingsCache oCache;

            oCache = new TProgramSettingsCache();
            return oCache.Get();
        }

        /// <summary>
        /// 取得資料庫項目。
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        public static TDatabaseItem GetDatabaseItem(string databaseID)
        {
            TDatabaseSettings oSettings;

            if (StrFunc.IsEmpty(databaseID))
                new ArgumentNullException("databaseID");

            oSettings = GetDatabaseSettings();
            if (!oSettings.Items.Contains(databaseID))
                throw new TException("DatabaseID '{0}' not found", databaseID);

            return oSettings.Items[databaseID];
        }

        /// <summary>
        /// 取得資料庫結構設定。
        /// </summary>
        public static TDbSchemaSettings GetDbSchemaSettings()
        {
            TDbSchemaSettingsCache oCache;

            oCache = new TDbSchemaSettingsCache();
            return oCache.Get();
        }

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        public static TDbTable GetDbTable(string dbName, string tableName)
        {
            TDbTableCache oCache;

            oCache = new TDbTableCache();
            return oCache.Get(dbName, tableName);
        }

        /// <summary>
        /// 取得預設資料庫的資料表結構。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        public static TDbTable GetDbTable(string tableName)
        {
            return GetDbTable(BackendInfo.DatabaseID, tableName);
        }

        /// <summary>
        /// 取得表單定義。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        public static TFormDefine GetFormDefine(string progID)
        {
            TFormDefineCache oCache;

            oCache = new TFormDefineCache();
            return oCache.Get(progID);
        }

        /// <summary>
        /// 取得表單版面配置。
        /// </summary>
        /// <param name="layoutID">版面代碼。</param>
        public static TFormLayout GetFormLayout(string layoutID)
        {
            var cache = new TFormLayoutCache();
            return cache.Get(layoutID);
        }

        /// <summary>
        /// 由快取區取得連線資訊。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public static TSessionInfo GetSessionInfo(Guid accessToken)
        {
            var cache = new TSessionInfoCache();
            return cache.Get(accessToken.ToString());
        }

        /// <summary>
        /// 將連線資訊置入快取。
        /// </summary>
        /// <param name="sessionInfo">連線資訊。</param>
        public static void SetSessionInfo(TSessionInfo sessionInfo)
        {
            var cache = new TSessionInfoCache();
            cache.Set(sessionInfo);
        }

        /// <summary>
        /// 由快取區移除連線資訊。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public static void RemoveSessionInfo(Guid accessToken)
        {
            var cache = new TSessionInfoCache();
            cache.Remove(accessToken.ToString());
        }

        /// <summary>
        /// 儲存頁面狀態至快取。
        /// </summary>
        /// <param name="uniqueGUID">頁面識別。</param>
        /// <param name="viewState">頁面狀態。</param>
        public static void SaveViewState(Guid uniqueGUID, object viewState)
        {
            TViewStateCache oCache;

            oCache = new TViewStateCache();
            oCache.Set(uniqueGUID.ToString(), viewState);
        }

        /// <summary>
        /// 由快取載入頁面狀態。
        /// </summary>
        /// <param name="uniqueGUID">頁面識別。</param>
        public static object LoadViewState(Guid uniqueGUID)
        {
            TViewStateCache oCache;

            oCache = new TViewStateCache();
            return oCache.Get(uniqueGUID.ToString());
        }
    }
}
