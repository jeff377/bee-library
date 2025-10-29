using Bee.Base;
using Bee.Define;
using System;
using System.Data;
using System.Text;

namespace Bee.Db
{
    /// <summary>
    /// 以 <see cref="DbTable"/> 為依據，產生 Insert、Update、Delete 的資料庫命令；亦可直接包裝成 <see cref="DataTableUpdateSpec"/>。
    /// </summary>
    public class DbTableCommandBuilder
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        /// <param name="dbTable">資料表結構。</param>
        public DbTableCommandBuilder(DatabaseType databaseType, DbTable dbTable)
        {
            DatabaseType = databaseType;
            DbTable = dbTable ?? throw new ArgumentNullException(nameof(dbTable), "DbTable cannot be null.");
        }

        /// <summary>
        /// 建構函式（由全域設定取得資料庫類型）。
        /// </summary>
        /// <param name="dbTable">資料表結構。</param>
        public DbTableCommandBuilder(DbTable dbTable)
            : this(BackendInfo.DatabaseType, dbTable)
        { }

        /// <summary>
        /// 資料庫類型。
        /// </summary>
        public DatabaseType DatabaseType { get; }

        /// <summary>
        /// 資料表結構。
        /// </summary>
        public DbTable DbTable { get; }

        /// <summary>
        /// 依據資料庫類型，回傳適當的識別字串跳脫格式。
        /// </summary>
        /// <param name="identifier">識別字名稱。</param>
        /// <returns>跳脫後的識別字。</returns>
        private string QuoteIdentifier(string identifier)
        {
            return DbFunc.QuoteIdentifier(DatabaseType, identifier);
        }

        /// <summary>
        /// 取得含前綴符號的參數名稱。
        /// </summary>
        /// <param name="name">不含前綴符號的參數名稱。</param>
        private string GetParameterName(string name)
        {
            return DbFunc.GetParameterName(DatabaseType, name);
        }

        /// <summary>
        /// 建立 Insert 語法的資料庫命令描述。
        /// </summary>
        public DbCommandSpec BuildInsertCommand()
        {
            var command = new DbCommandSpec();
            var buffer = new StringBuilder();
            string tableName = QuoteIdentifier(this.DbTable.TableName);
            buffer.AppendLine($"Insert Into {tableName} ");

            // 處理 Insert 的欄位名稱
            buffer.Append("(");
            int count = 0;
            foreach (DbField field in this.DbTable.Fields)
            {
                if (field.DbType != FieldDbType.AutoIncrement)
                {
                    if (count > 0)
                        buffer.Append(", ");
                    buffer.Append(QuoteIdentifier(field.FieldName));
                    count++;
                }
            }
            buffer.AppendLine(")");

            // 處理 Insert 的欄位值
            buffer.AppendLine(" Values ");
            buffer.Append("(");
            count = 0;
            foreach (DbField field in this.DbTable.Fields)
            {
                if (field.DbType != FieldDbType.AutoIncrement)
                {
                    if (count > 0)
                        buffer.Append(", ");
                    buffer.Append(GetParameterName(field.FieldName));
                    command.Parameters.Add(field); // 加入命令參數
                    count++;
                }
            }
            buffer.AppendLine(")");

            command.CommandText = buffer.ToString();
            return command;
        }

        /// <summary>
        /// 建立 Update 語法的資料庫命令描述。
        /// </summary>
        public DbCommandSpec BuildUpdateCommand()
        {
            var command = new DbCommandSpec();
            var buffer = new StringBuilder();
            string tableName = QuoteIdentifier(this.DbTable.TableName);
            buffer.AppendLine($"Update {tableName} Set ");

            string fieldName;
            // 取得主鍵欄位
            var keyField = this.DbTable.Fields[SysFields.RowId];
            // 處理 Update 的欄位名稱與值
            int iCount = 0;
            foreach (DbField field in this.DbTable.Fields)
            {
                if (field != keyField && field.DbType != FieldDbType.AutoIncrement)
                {
                    fieldName = QuoteIdentifier(field.FieldName);
                    // 加入命令參數
                    command.Parameters.Add(field);
                    if (iCount > 0)
                        buffer.Append(", ");
                    buffer.Append($"{fieldName}={GetParameterName(field.FieldName)}");
                    iCount++;
                }
            }
            // Where 加入主鍵條件
            fieldName = QuoteIdentifier(keyField.FieldName);
            command.Parameters.Add(keyField, System.Data.DataRowVersion.Original);
            buffer.AppendLine();
            buffer.AppendLine($"Where {fieldName}={GetParameterName(keyField.FieldName)}");

            command.CommandText = buffer.ToString();
            return command;
        }

        /// <summary>
        /// 建立 Delete 語法的資料庫命令描述。
        /// </summary>
        public DbCommandSpec BuildDeleteCommand()
        {
            var command = new DbCommandSpec();
            var buffer = new StringBuilder();
            string tableName = QuoteIdentifier(this.DbTable.TableName);
            buffer.AppendLine($"Delete From {tableName} ");

            // Where 加入主鍵條件
            var keyField = this.DbTable.Fields[SysFields.RowId];
            string fieldName = QuoteIdentifier(keyField.FieldName);
            command.Parameters.Add(keyField, System.Data.DataRowVersion.Original);
            buffer.AppendLine($"Where {fieldName}={GetParameterName(keyField.FieldName)}");

            command.CommandText = buffer.ToString();
            return command;
        }

        /// <summary>
        /// 直接產生 <see cref="DataTableUpdateSpec"/>，將 Insert/Update/Delete 命令與資料表一併打包。
        /// </summary>
        /// <param name="dataTable">要寫回資料庫的資料表。</param>
        /// <returns>批次異動規格。</returns>
        public DataTableUpdateSpec BuildUpdateSpec(DataTable dataTable)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable), "DataTable cannot be null.");

            var insertCmd = BuildInsertCommand();
            var updateCmd = BuildUpdateCommand();
            var deleteCmd = BuildDeleteCommand();

            return new DataTableUpdateSpec()
            {
                DataTable = dataTable,
                InsertCommand = insertCmd, 
                UpdateCommand = updateCmd,
                DeleteCommand = deleteCmd
            };
        }
    }
}
