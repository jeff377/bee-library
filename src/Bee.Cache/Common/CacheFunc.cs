using System;
using System.Collections.Generic;
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
            var cachePolicy = new CacheItemPolicy();
            if (policy.AbsoluteExpiration != DateTimeOffset.MaxValue)
                cachePolicy.AbsoluteExpiration = policy.AbsoluteExpiration;
            if (policy.SlidingExpiration != TimeSpan.Zero)
                cachePolicy.SlidingExpiration = policy.SlidingExpiration;
            if (policy.ChangeMonitorFilePaths != null)
                cachePolicy.ChangeMonitors.Add(new HostFileChangeMonitor(policy.ChangeMonitorFilePaths));
            return cachePolicy;
        }

        /// <summary>
        /// 取得系統設定。
        /// </summary>
        public static TSystemSettings GetSystemSettings()
        {
            return CacheContainer.SystemSettings.Get();
        }

        /// <summary>
        /// 取得資料庫設定。
        /// </summary>
        public static TDatabaseSettings GetDatabaseSettings()
        {
            return CacheContainer.DatabaseSettings.Get();
        }

        /// <summary>
        /// 取得程式清單。
        /// </summary>
        public static TProgramSettings GetProgramSettings()
        {
            return CacheContainer.ProgramSettings.Get();
        }

        /// <summary>
        /// 取得資料庫項目。
        /// </summary>
        /// <param name="databaseID">資料庫編號。</param>
        public static TDatabaseItem GetDatabaseItem(string databaseID)
        {
            if (StrFunc.IsEmpty(databaseID))
                throw new ArgumentNullException(nameof(databaseID));

            var settings = GetDatabaseSettings();
            if (!settings.Items.Contains(databaseID))
                throw new KeyNotFoundException($"DatabaseID '{databaseID}' not found.");

            return settings.Items[databaseID];
        }

        /// <summary>
        /// 取得資料庫結構設定。
        /// </summary>
        public static TDbSchemaSettings GetDbSchemaSettings()
        {
            return CacheContainer.DbSchemaSettings.Get();
        }

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        public static TDbTable GetDbTable(string dbName, string tableName)
        {
            return CacheContainer.DbTable.Get(dbName, tableName);
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
            var cache = new TFormDefineCache();
            return cache.Get(progID);
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
            var cache = new TViewStateCache();
            cache.Set(uniqueGUID.ToString(), viewState);
        }

        /// <summary>
        /// 由快取載入頁面狀態。
        /// </summary>
        /// <param name="uniqueGUID">頁面識別。</param>
        public static object LoadViewState(Guid uniqueGUID)
        {
            var cache = new TViewStateCache();
            return cache.Get(uniqueGUID.ToString());
        }
    }
}
