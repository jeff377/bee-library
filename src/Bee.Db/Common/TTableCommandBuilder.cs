using System.Data.Common;
using System.Text;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// 依結構資料表為依據，產生資料庫命令。
    /// </summary>
    public class TTableCommandBuilder
    {
        private readonly DbTable _DbTable = null;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="dbTable">資料表結構。</param>
        public TTableCommandBuilder(DbTable dbTable)
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
        private IDbCommandHelper CreateDbCommandHelper()
        {
            return DbFunc.CreateDbCommandHelper();
        }

        /// <summary>
        /// 建立 Insert 語法的資料庫命令。
        /// </summary>
        public DbCommand BuildInsertCommand()
        {
            IDbCommandHelper oHelper;
            StringBuilder oBuffer;
            string sTableName;
            int iCount;

            // 建立資料庫命令輔助類別
            oHelper = this.CreateDbCommandHelper();

            oBuffer = new StringBuilder();
            sTableName = oHelper.QuoteIdentifier(this.DbTable.TableName);
            oBuffer.AppendLine($"Insert Into {sTableName} ");

            // 處理 Insert 的欄位名稱
            oBuffer.Append("(");
            iCount = 0;
            foreach (DbField field in this.DbTable.Fields)
            {
                if (field.DbType != FieldDbType.Identity)
                {
                    if (iCount > 0)
                        oBuffer.Append(", ");
                    oBuffer.Append(oHelper.QuoteIdentifier(field.FieldName));
                    iCount++;
                }
            }
            oBuffer.AppendLine(")");

            // 處理 Insert 的欄位值
            oBuffer.AppendLine(" Values ");
            oBuffer.Append("(");
            iCount = 0;
            foreach (DbField field in this.DbTable.Fields)
            {
                if (field.DbType != FieldDbType.Identity)
                {
                    if (iCount > 0)
                        oBuffer.Append(", ");
                    oBuffer.Append(oHelper.GetParameterName(field.FieldName));
                    oHelper.AddParameter(field); // 加入命令參數
                    iCount++;
                }
            }
            oBuffer.AppendLine(")");

            oHelper.SetCommandText(oBuffer.ToString());
            return oHelper.DbCommand;
        }

        /// <summary>
        /// 建立 Update 語法的資料庫命令。
        /// </summary>
        public DbCommand BuildUpdateCommand()
        {
            IDbCommandHelper oHelper;
            StringBuilder oBuffer;
            DbParameter oParameter;
            DbField oKeyField;
            string sTableName, sFieldName;
            int iCount;

            // 建立資料庫命令輔助類別
            oHelper = this.CreateDbCommandHelper();

            oBuffer = new StringBuilder();
            sTableName = oHelper.QuoteIdentifier(this.DbTable.TableName);
            oBuffer.AppendLine($"Update {sTableName} Set ");

            // 取得主鍵欄位
            oKeyField = this.DbTable.Fields[SysFields.RowId];
            // 處理 Update 的欄位名稱與值
            iCount = 0;
            foreach (DbField field in this.DbTable.Fields)
            {
                if (field != oKeyField && field.DbType != FieldDbType.Identity)
                {
                    sFieldName = oHelper.QuoteIdentifier(field.FieldName);
                    // 加入命令參數
                    oParameter = oHelper.AddParameter(field);
                    if (iCount > 0)
                        oBuffer.Append(", ");
                    oBuffer.Append($"{sFieldName}={oParameter.ParameterName}");
                    iCount++;
                }
            }
            // Where 加入主鍵條件
            sFieldName = oHelper.QuoteIdentifier(oKeyField.FieldName);
            oParameter = oHelper.AddParameter(oKeyField, System.Data.DataRowVersion.Original);
            oBuffer.AppendLine();
            oBuffer.AppendLine($"Where {sFieldName}={oParameter.ParameterName}");

            oHelper.SetCommandText(oBuffer.ToString());
            return oHelper.DbCommand; ;
        }

        /// <summary>
        /// 建立 Delete 語法的資料庫命令。
        /// </summary>
        public DbCommand BuildDeleteCommand()
        {
            IDbCommandHelper oHelper;
            StringBuilder oBuffer;
            DbParameter oParameter;
            DbField oKeyField;
            string sTableName, sFieldName;

            // 建立資料庫命令輔助類別
            oHelper = this.CreateDbCommandHelper();

            oBuffer = new StringBuilder();
            sTableName = oHelper.QuoteIdentifier(this.DbTable.TableName);
            oBuffer.AppendLine($"Delete From {sTableName} ");

            // Where 加入主鍵條件
            oKeyField = this.DbTable.Fields[SysFields.RowId];
            sFieldName = oHelper.QuoteIdentifier(oKeyField.FieldName);
            oParameter = oHelper.AddParameter(oKeyField, System.Data.DataRowVersion.Original);
            oBuffer.AppendLine($"Where {sFieldName}={oParameter.ParameterName}");

            oHelper.SetCommandText(oBuffer.ToString());
            return oHelper.DbCommand; ;
        }
    }
}
