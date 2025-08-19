using System.Data.Common;
using System.Text;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 依結構資料表為依據，產生資料庫命令。
    /// </summary>
    public class TableCommandBuilder
    {
        private readonly DbTable _DbTable = null;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="dbTable">資料表結構。</param>
        public TableCommandBuilder(DbTable dbTable)
        {
            _DbTable = dbTable;
        }

        /// <summary>
        /// 資料表結構。
        /// </summary>
        public DbTable DbTable
        {
            get { return _DbTable; }
        }

        /// <summary>
        /// 建立資料庫命令輔助類別。
        /// </summary>
        private DbCommandHelper CreateDbCommandHelper()
        {
            return DbFunc.CreateDbCommandHelper();
        }

        /// <summary>
        /// 建立 Insert 語法的資料庫命令。
        /// </summary>
        public DbCommand BuildInsertCommand()
        {
            var helper = this.CreateDbCommandHelper();
            var buffer = new StringBuilder();
            string tableName = helper.QuoteIdentifier(this.DbTable.TableName);
            buffer.AppendLine($"Insert Into {tableName} ");

            // 處理 Insert 的欄位名稱
            buffer.Append("(");
            int count = 0;
            foreach (DbField field in this.DbTable.Fields)
            {
                if (field.DbType != FieldDbType.Identity)
                {
                    if (count > 0)
                        buffer.Append(", ");
                    buffer.Append(helper.QuoteIdentifier(field.FieldName));
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
                if (field.DbType != FieldDbType.Identity)
                {
                    if (count > 0)
                        buffer.Append(", ");
                    buffer.Append(helper.GetParameterName(field.FieldName));
                    helper.AddParameter(field); // 加入命令參數
                    count++;
                }
            }
            buffer.AppendLine(")");

            helper.SetCommandText(buffer.ToString());
            return helper.DbCommand;
        }

        /// <summary>
        /// 建立 Update 語法的資料庫命令。
        /// </summary>
        public DbCommand BuildUpdateCommand()
        {
            var helper = this.CreateDbCommandHelper();
            var buffer = new StringBuilder();
            string tableName = helper.QuoteIdentifier(this.DbTable.TableName);
            buffer.AppendLine($"Update {tableName} Set ");

            DbParameter parameter;
            string fieldName;
            // 取得主鍵欄位
            var keyField = this.DbTable.Fields[SysFields.RowId];
            // 處理 Update 的欄位名稱與值
            int iCount = 0;
            foreach (DbField field in this.DbTable.Fields)
            {
                if (field != keyField && field.DbType != FieldDbType.Identity)
                {
                    fieldName = helper.QuoteIdentifier(field.FieldName);
                    // 加入命令參數
                    parameter = helper.AddParameter(field);
                    if (iCount > 0)
                        buffer.Append(", ");
                    buffer.Append($"{fieldName}={parameter.ParameterName}");
                    iCount++;
                }
            }
            // Where 加入主鍵條件
            fieldName = helper.QuoteIdentifier(keyField.FieldName);
            parameter = helper.AddParameter(keyField, System.Data.DataRowVersion.Original);
            buffer.AppendLine();
            buffer.AppendLine($"Where {fieldName}={parameter.ParameterName}");

            helper.SetCommandText(buffer.ToString());
            return helper.DbCommand; ;
        }

        /// <summary>
        /// 建立 Delete 語法的資料庫命令。
        /// </summary>
        public DbCommand BuildDeleteCommand()
        {
            var helper = this.CreateDbCommandHelper();
            var buffer = new StringBuilder();
            string sTableName = helper.QuoteIdentifier(this.DbTable.TableName);
            buffer.AppendLine($"Delete From {sTableName} ");

            // Where 加入主鍵條件
            var keyField = this.DbTable.Fields[SysFields.RowId];
            string sFieldName = helper.QuoteIdentifier(keyField.FieldName);
            var oParameter = helper.AddParameter(keyField, System.Data.DataRowVersion.Original);
            buffer.AppendLine($"Where {sFieldName}={oParameter.ParameterName}");

            helper.SetCommandText(buffer.ToString());
            return helper.DbCommand; ;
        }
    }
}
