using System;
using System.Data.Common;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// SQL Server 資料庫建立表單相關命令語法產生器，包含 Select、Insert、Update、Delete 語法。
    /// </summary>
    public class TSqlFormCommandBuilder : IFormCommandBuilder
    {
        private readonly FormDefine _FormDefine = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TSqlFormCommandBuilder(FormDefine formDefine)
        {
            _FormDefine = formDefine;
        }

        #endregion

        /// <summary>
        /// 表單定義。
        /// </summary>
        public FormDefine FormDefine
        {
            get { return _FormDefine; }
        }

        /// <summary>
        /// 建立 Select 語法的資料庫命令。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        /// <param name="selectFields">要取得的欄位集合字串，以逗點分隔欄位名稱，空字串表示取得所有欄位。</param>
        public DbCommand BuildSelectCommand(string tableName, string selectFields)
        {
            TSqlFormSelectCommandBuilder oBuilder;

            oBuilder = new TSqlFormSelectCommandBuilder(this.FormDefine);
            return oBuilder.Execute(tableName, selectFields);
        }

        /// <summary>
        /// 建立 Insert 語法的資料庫命令。
        /// </summary>
        public DbCommand BuildInsertCommand()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 建立 Update 語法的資料庫命令。
        /// </summary>
        public DbCommand BuildUpdateCommand()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 建立 Delete 語法的資料庫命令。
        /// </summary>
        public DbCommand BuildDeleteCommand()
        {
            throw new NotSupportedException();
        }
    }
}
