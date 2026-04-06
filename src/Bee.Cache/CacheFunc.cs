using Bee.Define.Database;
using Bee.Define.Forms;
using Bee.Define.Layouts;
using Bee.Define.Settings;
using System;
using System.Runtime.Caching;
using Bee.Define;

namespace Bee.Cache
{
    /// <summary>
    /// Cache utility library.
    /// </summary>
    public static class CacheFunc
    {
        /// <summary>
        /// Creates the eviction policy for a cache item.
        /// </summary>
        /// <param name="policy">The eviction policy for the cache item.</param>
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
        /// Gets the system settings.
        /// </summary>
        public static SystemSettings GetSystemSettings()
        {
            return CacheContainer.SystemSettings.Get();
        }

        /// <summary>
        /// Gets the database settings.
        /// </summary>
        public static DatabaseSettings GetDatabaseSettings()
        {
            return CacheContainer.DatabaseSettings.Get();
        }

        /// <summary>
        /// Gets the program settings.
        /// </summary>
        public static ProgramSettings GetProgramSettings()
        {
            return CacheContainer.ProgramSettings.Get();
        }

        /// <summary>
        /// Gets the database schema settings.
        /// </summary>
        public static DbSchemaSettings GetDbSchemaSettings()
        {
            return CacheContainer.DbSchemaSettings.Get();
        }

        /// <summary>
        /// Gets the table schema for the specified table.
        /// </summary>
        /// <param name="dbName">The database name.</param>
        /// <param name="tableName">The table name.</param>
        public static TableSchema GetTableSchema(string dbName, string tableName)
        {
            return CacheContainer.TableSchema.Get(dbName, tableName);
        }

        /// <summary>
        /// Gets the table schema for the specified table in the default database.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        public static TableSchema GetTableSchema(string tableName)
        {
            return GetTableSchema(BackendInfo.DatabaseId, tableName);
        }

        /// <summary>
        /// Gets the form schema definition for the specified program.
        /// </summary>
        /// <param name="progId">The program identifier.</param>
        public static FormSchema GetFormSchema(string progId)
        {
            return CacheContainer.FormSchema.Get(progId);
        }

        /// <summary>
        /// Gets the form layout for the specified layout identifier.
        /// </summary>
        /// <param name="layoutId">The layout identifier.</param>
        public static FormLayout GetFormLayout(string layoutId)
        {
            return CacheContainer.FormLayout.Get(layoutId);
        }

        /// <summary>
        /// Gets the session information from the cache.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public static SessionInfo GetSessionInfo(Guid accessToken)
        {
            return CacheContainer.SessionInfo.Get(accessToken);
        }

        /// <summary>
        /// Stores the session information in the cache.
        /// </summary>
        /// <param name="sessionInfo">The session information.</param>
        public static void SetSessionInfo(SessionInfo sessionInfo)
        {
            CacheContainer.SessionInfo.Set(sessionInfo);
        }

        /// <summary>
        /// Removes the session information from the cache.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public static void RemoveSessionInfo(Guid accessToken)
        {
            CacheContainer.SessionInfo.Remove(accessToken);
        }

        /// <summary>
        /// Saves the view state to the cache.
        /// </summary>
        /// <param name="uniqueGuid">The page identifier.</param>
        /// <param name="viewState">The view state.</param>
        public static void SaveViewState(Guid uniqueGuid, object viewState)
        {
            CacheContainer.ViewState.Set(uniqueGuid, viewState);
        }

        /// <summary>
        /// Loads the view state from the cache.
        /// </summary>
        /// <param name="uniqueGuid">The page identifier.</param>
        public static object LoadViewState(Guid uniqueGuid)
        {
            return CacheContainer.ViewState.Get(uniqueGuid);
        }
    }
}
