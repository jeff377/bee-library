using Bee.Define;
using System;

namespace Bee.Db
{
    /// <summary>
    /// SQL Server 資料庫建立表單相關命令語法產生器，包含 Select、Insert、Update、Delete 語法。
    /// </summary>
    public class SqlFormCommandBuilder : IFormCommandBuilder
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public SqlFormCommandBuilder(FormDefine formDefine)
        {
            FormDefine = formDefine ?? throw new ArgumentNullException(nameof(formDefine));
        }

        #endregion

        /// <summary>
        /// 表單定義。
        /// </summary>
        private FormDefine FormDefine { get; }

        /// <summary>
        /// 建立 Select 語法的資料庫命令。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        /// <param name="selectFields">要取得的欄位集合字串，以逗點分隔欄位名稱，空字串表示取得所有欄位。</param>
        /// <param name="filter">過濾條件。</param>
        /// <param name="sortFields">排序欄位集合。</param>
        public DbCommandSpec BuildSelectCommand(string tableName, string selectFields, FilterNode filter = null, SortFIeldCollection sortFields = null)
        {
            var builder = new SqlSelectCommandBuilder(FormDefine);
            return builder.Build(tableName, selectFields, filter, sortFields);  
        }

        /// <summary>
        /// 建立 Insert 語法的資料庫命令。
        /// </summary>
        public DbCommandSpec BuildInsertCommand()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 建立 Update 語法的資料庫命令。
        /// </summary>
        public DbCommandSpec BuildUpdateCommand()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 建立 Delete 語法的資料庫命令。
        /// </summary>
        public DbCommandSpec BuildDeleteCommand()
        {
            throw new NotSupportedException();
        }
    }
}
