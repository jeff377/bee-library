using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 資料庫存取物件。
    /// </summary>
    public class DbAccess
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="provider">資料庫來源提供者。</param>
        /// <param name="connectionString">資料庫連線字串。</param>
        public DbAccess(DbProviderFactory provider, string connectionString)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            Provider = provider;
            ConnectionString = connectionString;
        }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="database">資料庫連線定義。</param>
        public DbAccess(DatabaseItem database)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            Provider = DbProviderManager.GetFactory(database.DatabaseType)
                       ?? throw new InvalidOperationException($"Unknown database type: {database.DatabaseType}.");
            ConnectionString = database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new InvalidOperationException("DatabaseItem.GetConnectionString() returned null or empty.");
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
            if (connection == null)
                throw new InvalidOperationException("Failed to create a database connection: DbProviderFactory.CreateConnection() returned null.");

            connection.ConnectionString = this.ConnectionString;
            return connection;
        }

        /// <summary>
        /// 開啟資料庫連線。
        /// </summary>
        private DbConnection OpenConnection()
        {
            var connection = CreateConnection();
            try
            {
                connection.Open();
                return connection;
            }
            catch
            {
                // 失敗時也要確保釋放
                connection.Dispose();
                throw;
            }
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
            using (var connection = this.OpenConnection())
            {
                command.Connection = connection;
                using (var adapter = this.Provider.CreateDataAdapter())
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
            using (var command = CreateCommand(commandText))
            {
                return ExecuteDataTable(command); // command 將於此 using 結束時 Dispose
            }
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
            using (var command = CreateCommand(commandText))
            {
                return ExecuteNonQuery(command);
            }
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
            using (var command = CreateCommand(commandText))
            {
                return ExecuteScalar(command);
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// 呼叫端需在使用完畢後呼叫 reader.Dispose()
        /// </summary>
        /// <param name="command">資料庫命令。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        public DbDataReader ExecuteReader(DbCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var connection = OpenConnection();
            try
            {
                command.Connection = connection;
                // CloseConnection: reader 關閉時一併關閉 connection
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch
            {
                // ExecuteReader 失敗要自行清理連線
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 執行資料庫命令，傳回 DbDataReader 以便進一步處理資料。
        /// 呼叫端需在使用完畢後呼叫 reader.Dispose()
        /// </summary>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <returns>傳回 DbDataReader 物件。</returns>
        public DbDataReader ExecuteReader(string commandText)
        {
            // Reader 交給呼叫端釋放，因此這裡不能 using command
            var command = CreateCommand(commandText);
            return ExecuteReader(command);
        }

        /// <summary>
        /// 執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的可列舉集合。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="command">資料庫命令。</param>
        /// <returns>
        /// 返回 <see cref="IEnumerable{T}"/>，允許逐筆讀取查詢結果。
        /// </returns>
        public IEnumerable<T> Query<T>(DbCommand command)
        {
            // 使用 command 執行資料庫查詢，並取得 DbDataReader
            var reader = ExecuteReader(command);
            var mapper = ILMapper<T>.CreateMapFunc(reader);
            // 延遲執行，不能使用 using，會造成連線被提早關閉
            try
            {
                foreach (var item in ILMapper<T>.MapToEnumerable(reader, mapper))
                {
                    yield return item;
                }
            }
            finally
            {
                reader.Dispose(); // 迭代結束後才關閉 reader
            }
        }

        /// <summary>
        /// 執行資料庫命令，並將結果逐筆映射為指定類型 <typeparamref name="T"/> 的可列舉集合。
        /// </summary>
        /// <typeparam name="T">要映射的目標類型。</typeparam>
        /// <param name="commandText">SQL 陳述式。</param>
        /// <returns>
        /// 返回 <see cref="IEnumerable{T}"/>，允許逐筆讀取查詢結果。
        /// </returns>
        public IEnumerable<T> Query<T>(string commandText)
        {
            using (var command = CreateCommand(commandText))
            {
                var reader = ExecuteReader(command); // reader.Dispose 時會關 connection
                var mapper = ILMapper<T>.CreateMapFunc(reader);
                try
                {
                    foreach (var item in ILMapper<T>.MapToEnumerable(reader, mapper))
                        yield return item;
                }
                finally
                {
                    reader.Dispose(); // 於列舉完成時關閉
                }
            } // command 於列舉完成時 Dispose
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
            if (dataTable == null) throw new ArgumentNullException(nameof(dataTable));

            using (var connection = OpenConnection())
            {
                if (insertCommand != null) insertCommand.Connection = connection;
                if (updateCommand != null) updateCommand.Connection = connection;
                if (deleteCommand != null) deleteCommand.Connection = connection;

                var adapter = Provider.CreateDataAdapter();
                if (adapter == null)
                    throw new InvalidOperationException("DbProviderFactory.CreateDataAdapter() returned null.");

                using (adapter)
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
            using (var connection = CreateConnection())
            {
                connection.Open();
            }
        }
    }
}
