using System.Data;
using System.Data.Common;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫存取物件。
    /// </summary>
    public class TDbAccess
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="provider">資料庫來源提供者。</param>
        /// <param name="connectionString">資料庫連線字串。</param>
        public TDbAccess(DbProviderFactory provider, string connectionString)
        {
            Provider = provider;
            ConnectionString = connectionString;
        }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="database">資料庫連線定義。</param>
        public TDbAccess(TDatabaseItem database)
        {
            Provider = DbProviderManager.GetFactory(database.DatabaseType);
            ConnectionString = database.GetConnectionString();
        }

        #endregion

        /// <summary>
        /// 資料庫來源提供者。
        /// </summary>
        public DbProviderFactory Provider { get; private set; }

        /// <summary>
        /// 資料庫連線字串。
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// 建立資料庫連線。
        /// </summary>
        public DbConnection CreateConnection()
        {
            var connection = this.Provider.CreateConnection();
            connection.ConnectionString = this.ConnectionString;
            return connection;
        }

        /// <summary>
        /// 開啟資料庫連線。
        /// </summary>
        private DbConnection OpenConnection()
        {
            var connection = CreateConnection();
            connection.Open();
            return connection;
        }

        /// <summary>
        /// 建立資料庫命令。
        /// </summary>
        /// <param name="commandType">SQL 命令類型。</param>
        /// <param name="commandText">SQL 陳述式或預存程序。</param>
        private DbCommand CreateCommand(CommandType commandType, string commandText)
        {
            var command = this.Provider.CreateCommand();
            command.CommandType = commandType;
            command.CommandText = commandText;
            return command;
        }

        /// <summary>
        /// 建立資料庫命令。
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        private DbCommand CreateCommand(string commandText)
        {
            return CreateCommand(CommandType.Text, commandText);
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        public DataTable ExecuteDataTable(DbCommand command)
        {
            DataTable table;
            using (DbConnection connection = this.OpenConnection())
            {
                command.Connection = connection;
                using (DbDataAdapter adapter = this.Provider.CreateDataAdapter())
                {
                    adapter.SelectCommand = command;
                    table = new DataTable("DataTable");
                    adapter.Fill(table);
                }
            }
            DataSetFunc.UpperColumnName(table);
            return table;
        }

        /// <summary>
        /// 執行資料庫命令，傳回資料表。
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        public DataTable ExecuteDataTable(string commandText)
        {
            var command = this.CreateCommand(commandText);
            return ExecuteDataTable(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        public int ExecuteNonQuery(DbCommand command)
        {
            int rowsAffected = 0;  // 異動筆數
            using (DbConnection connection = this.OpenConnection())
            {
                command.Connection = connection;
                rowsAffected = command.ExecuteNonQuery();
            }
            return rowsAffected;
        }

        /// <summary>
        /// 執行資料庫命令，傳回異動筆數。
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        public int ExecuteNonQuery(string commandText)
        {
            var command = this.CreateCommand(commandText);
            return ExecuteNonQuery(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        public object ExecuteScalar(DbCommand command)
        {
            object value;
            using (DbConnection connection = this.OpenConnection())
            {
                command.Connection = connection;
                value = command.ExecuteScalar();
            }
            return value;
        }

        /// <summary>
        /// 執行資料庫命令，傳回單一值。
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        public object ExecuteScalar(string commandText)
        {
            var command = this.CreateCommand(commandText);
            return ExecuteScalar(command);
        }

        /// <summary>
        /// 執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        public DbDataReader ExecuteReader(DbCommand command)
        {
            var connection = this.OpenConnection();
            command.Connection = connection;
            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        /// <summary>
        /// 執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        public DbDataReader ExecuteReader(string commandText)
        {
            var command = this.CreateCommand(commandText);
            return ExecuteReader(command);
        }

        /// <summary>
        /// 將 DataTable 的異動寫入資料庫。 
        /// </summary>
        /// <param name="dataTable">資料表。</param>
        /// <param name="insertCommand">新增命令。</param>
        /// <param name="updateCommand">更新命令。</param>
        /// <param name="deleteCommand">刪除命令。</param>
        public int UpdateDataTable(DataTable dataTable, DbCommand insertCommand, DbCommand updateCommand, DbCommand deleteCommand)
        {
            using (DbConnection connection = this.OpenConnection())
            {
                if (insertCommand != null)
                    insertCommand.Connection = connection;
                if (updateCommand != null)
                    updateCommand.Connection = connection;
                if (deleteCommand != null)
                    deleteCommand.Connection = connection;

                using (DbDataAdapter adapter = this.Provider.CreateDataAdapter())
                {
                    adapter.InsertCommand = insertCommand;
                    adapter.UpdateCommand = updateCommand;
                    adapter.DeleteCommand = deleteCommand;
                    return adapter.Update(dataTable);
                }
            }
        }

        /// <summary>
        /// 測試資料庫連線。
        /// </summary>
        public void TestConnection()
        {
            using (DbConnection connection = this.CreateConnection())
            {
                connection.Open();
                connection.Close();
            }
        }
    }
}
