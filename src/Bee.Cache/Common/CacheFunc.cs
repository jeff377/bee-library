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
        internal static System.Runtime.Caching.CacheItemPolicy CreateCachePolicy(CacheItemPolicy policy)
        {
            var cachePolicy = new System.Runtime.Caching.CacheItemPolicy();
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
        public static SystemSettings GetSystemSettings()
        {
            return CacheContainer.SystemSettings.Get();
        }

        /// <summary>
        /// 取得資料庫設定。
        /// </summary>
        public static DatabaseSettings GetDatabaseSettings()
        {
            return CacheContainer.DatabaseSettings.Get();
        }

        /// <summary>
        /// 取得程式清單。
        /// </summary>
        public static ProgramSettings GetProgramSettings()
        {
            return CacheContainer.ProgramSettings.Get();
        }

        /// <summary>
        /// 取得資料庫結構設定。
        /// </summary>
        public static DbSchemaSettings GetDbSchemaSettings()
        {
            return CacheContainer.DbSchemaSettings.Get();
        }

        /// <summary>
        /// 取得資料表結構。
        /// </summary>
        /// <param name="dbName">資料庫名稱。</param>
        /// <param name="tableName">資料表名稱。</param>
        public static DbTable GetDbTable(string dbName, string tableName)
        {
            return CacheContainer.DbTable.Get(dbName, tableName);
        }

        /// <summary>
        /// 取得預設資料庫的資料表結構。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        public static DbTable GetDbTable(string tableName)
        {
            return GetDbTable(BackendInfo.DatabaseId, tableName);
        }

        /// <summary>
        /// 取得表單定義。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        public static FormDefine GetFormDefine(string progId)
        {
            return CacheContainer.FormDefine.Get(progId);
        }

        /// <summary>
        /// 取得表單版面配置。
        /// </summary>
        /// <param name="layoutId">表單版面代碼。</param>
        public static FormLayout GetFormLayout(string layoutId)
        {
            return CacheContainer.FormLayout.Get(layoutId);
        }

        /// <summary>
        /// 由快取區取得連線資訊。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public static SessionInfo GetSessionInfo(Guid accessToken)
        {
            return CacheContainer.SessionInfo.Get(accessToken);
        }

        /// <summary>
        /// 將連線資訊置入快取。
        /// </summary>
        /// <param name="sessionInfo">連線資訊。</param>
        public static void SetSessionInfo(SessionInfo sessionInfo)
        {
            CacheContainer.SessionInfo.Set(sessionInfo);
        }

        /// <summary>
        /// 由快取區移除連線資訊。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        public static void RemoveSessionInfo(Guid accessToken)
        {
            CacheContainer.SessionInfo.Remove(accessToken);
        }

        /// <summary>
        /// 儲存頁面狀態至快取。
        /// </summary>
        /// <param name="uniqueGuid">頁面識別。</param>
        /// <param name="viewState">頁面狀態。</param>
        public static void SaveViewState(Guid uniqueGuid, object viewState)
        {
            CacheContainer.ViewState.Set(uniqueGuid, viewState);
        }

        /// <summary>
        /// 由快取載入頁面狀態。
        /// </summary>
        /// <param name="uniqueGuid">頁面識別。</param>
        public static object LoadViewState(Guid uniqueGuid)
        {
            return CacheContainer.ViewState.Get(uniqueGuid);
        }
    }
}
