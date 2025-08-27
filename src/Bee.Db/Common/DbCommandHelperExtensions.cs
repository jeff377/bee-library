using Bee.Define;
using System;
using System.Data;

namespace Bee.Db
{
    /// <summary>
    /// DbCommandHelper 的擴充方法。
    /// </summary>
    public static class DbCommandHelperExtensions
    {
        /// <summary>
        /// 執行資料庫命令，傳回資料表；執行完自動釋放 <see cref="DbCommandHelper.DbCommand"/>。
        /// </summary>
        /// <param name="helper">資料庫命令組裝輔助類別。</param>
        /// <param name="databaseId">資料庫編號。</param>
        public static DataTable ExecuteDataTable(this DbCommandHelper helper, string databaseId)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            if (helper.DbCommand == null) throw new InvalidOperationException("DbCommand is not initialized.");
            if (string.IsNullOrWhiteSpace(databaseId))
                throw new ArgumentException("databaseId cannot be null or empty.", nameof(databaseId));

            using (var command = helper.DbCommand)
            {
                return SysDb.ExecuteDataTable(databaseId, command);
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表；執行完自動釋放 <see cref="DbCommandHelper.DbCommand"/>。
        /// 使用預設的 <c>BackendInfo.DatabaseId</c>。
        /// </summary>
        /// <param name="helper">資料庫命令組裝輔助類別。</param>
        public static DataTable ExecuteDataTable(this DbCommandHelper helper)
        {
            return ExecuteDataTable(helper, BackendInfo.DatabaseId);
        }
    }
}
