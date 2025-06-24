using System.Data.Common;
using System.Text;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 表單的 Select 語法產生器。
    /// </summary>
    internal class SqlFormSelectCommandBuilder
    {
        private readonly FormDefine _FormDefine = null;
        private readonly IDbCommandHelper _Helper = null;
        private TableJoinProvider _TableJoinProvider = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public SqlFormSelectCommandBuilder(FormDefine formDefine)
        {
            _FormDefine = formDefine;
            _Helper = DbFunc.CreateDbCommandHelper(DatabaseType.SQLServer);
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
        /// 資料庫命令輔助類別。
        /// </summary>
        public IDbCommandHelper Helper
        {
            get { return _Helper; }
        }

        /// <summary>
        /// 資料表關連資訊提供者。
        /// </summary>
        public TableJoinProvider TableJoinProvider
        {
            get { return _TableJoinProvider; }
        }

        /// <summary>
        /// 建立資料表關連資訊提供者。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        /// <param name="selectFields">要取得的欄位集合字串，以逗點分隔欄位名稱，空字串表示取得所有欄位。</param>
        private TableJoinProvider CreateTableJoinProvider(string tableName, string selectFields)
        {
            TableJoinBuilder oBuilder;

            oBuilder = new TableJoinBuilder(this.Helper, this.FormDefine, tableName, selectFields);
            return oBuilder.Execute();
        }

        /// <summary>
        /// 建立資料庫命令。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        /// <param name="selectFields">要取得的欄位集合字串，以逗點分隔欄位名稱，空字串表示取得所有欄位。</param>
        public DbCommand Execute(string tableName, string selectFields)
        {
            FormTable oFormTable;
            StringBuilder oBuffer;
            string sCommandText;

            oFormTable = this.FormDefine.Tables[tableName];
            if (oFormTable == null) { return null; }

            // 建立資料表關連資訊提供者
            _TableJoinProvider = CreateTableJoinProvider(tableName, selectFields);

            oBuffer = new StringBuilder();
            // 取得 Select 部分的語法
            sCommandText = string.Empty;

            return null;
        }

    }
}
