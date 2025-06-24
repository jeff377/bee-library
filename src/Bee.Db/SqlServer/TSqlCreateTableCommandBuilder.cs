using System;
using System.Text;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// SQL Server 資料庫建立資料表命令語法產生器。
    /// </summary>
    public class TSqlCreateTableCommandBuilder : ICreateTableCommandBuilder
    {
        private DbTable _DbTable = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TSqlCreateTableCommandBuilder()
        { }

        #endregion

        /// <summary>
        /// 資料表結構。
        /// </summary>
        private DbTable DbTable
        {
            get { return _DbTable; }
        }

        /// <summary>
        /// 資料表名稱。
        /// </summary>
        private string TableName
        {
            get { return this.DbTable.TableName; }
        }

        /// <summary>
        /// 取得 Create Table 的 SQL 語法。
        /// </summary>
        /// <param name="dbTable">資料表結構。</param>
        public string GetCommandText(DbTable dbTable)
        {
            _DbTable = dbTable;

            if (this.DbTable.UpgradeAction == DbUpgradeAction.Upgrade)
                return $"-- 升級 {this.TableName} 資料表\r\n{this.GetUpgradeCommandText()}";
            else
                return $"-- 建立 {this.TableName} 資料表\r\n{this.GetCreateTableCommandText()}";
        }

        /// <summary>
        /// 取得升級舊資料表的 SQL 語法。
        /// </summary>
        private string GetUpgradeCommandText()
        {
            StringBuilder oBuffer;
            string sNewTableName;
            string sSQL;

            oBuffer = new StringBuilder();

            sNewTableName = $"tmp_{this.TableName}";
            // 刪除暫存資料表
            sSQL = GetDropTableCommandText(sNewTableName);
            oBuffer.AppendLine("-- 刪除暫存資料表");
            oBuffer.AppendLine(sSQL);
            // 建立暫存資料表
            sSQL = GetCreateTableCommandText(sNewTableName);
            oBuffer.AppendLine("-- 建立暫存資料表");
            oBuffer.AppendLine(sSQL);
            // 搬移資料
            sSQL = GetInsertTableCommandText(this.TableName, sNewTableName);
            oBuffer.AppendLine("-- 搬移資料");
            oBuffer.AppendLine(sSQL);
            // 刪除舊資料表
            sSQL = GetDropTableCommandText(this.TableName);
            oBuffer.AppendLine("-- 刪除舊資料表");
            oBuffer.AppendLine(sSQL);
            // 暫存資料表更名
            oBuffer.AppendLine("-- 暫存資料表更名");
            sSQL = GetRenameTableCommandText(sNewTableName, this.TableName);
            oBuffer.AppendLine(sSQL);

            return oBuffer.ToString();
        }

        /// <summary>
        /// 取得刪除資料表的命令文字。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        private string GetDropTableCommandText(string tableName)
        {
            return $"IF (SELECT COUNT(*) From sys.tables WHERE name=N'{tableName}')>0\n" +
                        $"  EXEC('DROP TABLE {tableName}');";
        }

        /// <summary>
        /// 取得搬移資料的 INSERT INTO SELECT 的 SQL 語法。
        /// </summary>
        /// <param name="tableName">舊資料表名稱。</param>
        /// <param name="newTableName">新資料表名稱。</param>
        private string GetInsertTableCommandText(string tableName, string newTableName)
        {
            string sSQL;
            string sFields;

            // 取得要搬除的欄位清單
            sFields = string.Empty;
            foreach (DbField field in this.DbTable.Fields)
            {
                if (field.UpgradeAction != DbUpgradeAction.New && field.DbType != FieldDbType.Identity)
                {
                    if (StrFunc.IsNotEmpty(sFields))
                        sFields += ", ";
                    sFields += $"[{field.FieldName}]";
                }
            }
            // 組成  INSERT INTO SELECT 語法
            sSQL = $"INSERT INTO [{newTableName}] ({sFields}) \n" +
                          $"SELECT {sFields} FROM [{tableName}];";
            return sSQL;
        }

        /// <summary>
        /// 取得資料表更名的命令文字。
        /// </summary>
        /// <param name="tableName">舊資料表名稱。</param>
        /// <param name="newTableName">新資料表名稱。</param>
        private string GetRenameTableCommandText(string tableName, string newTableName)
        {
            var sb = new StringBuilder();
            // 索引更名
            foreach (DbTableIndex index in this.DbTable.Indexes)
            {
                string oldName = StrFunc.Format(index.Name, tableName);  // 舊索引名稱
                string newName = StrFunc.Format(index.Name, newTableName);  // 新索引名稱 
                sb.Append($"EXEC sp_rename N'dbo.{tableName}.{oldName}', N'{newName}', N'INDEX';\n");
            }
            // 資料表更名
            sb.Append($"EXEC sp_rename N'{tableName}', N'{newTableName}';\n");
            return sb.ToString();
        }

        /// <summary>
        /// 取得 Create Table 的 SQL 語法。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        private string GetCreateTableCommandText(string tableName = "")
        {
            StringBuilder oBuffer;
            string sTableName;
            string sFields;
            string sPrimaryKey;
            string sIndexs;

            // 資料表名稱
            sTableName = StrFunc.IsNotEmpty(tableName) ? tableName : this.DbTable.TableName;
            // 取得建立欄位結構的語法
            sFields = GetFieldsCommandText();
            // 取得建立主索引的命令語法
            sPrimaryKey = GetPrimaryKeyCommandText(sTableName);
            // 取得建立索引的命令語法
            sIndexs = GetIndexsCommandText(sTableName);

            oBuffer = new StringBuilder();
            // 組成 Create Table 的語法
            oBuffer.Append($"CREATE TABLE [{sTableName}] (\r\n{sFields}");
            if (StrFunc.IsNotEmpty(sPrimaryKey))
                oBuffer.Append($",\r\n  {sPrimaryKey}");
            oBuffer.Append("\r\n);");
            // 加上建立索引語法
            if (StrFunc.IsNotEmpty(sIndexs))
                oBuffer.Append($"\r\n{sIndexs}");
            return oBuffer.ToString();
        }

        /// <summary>
        /// 取得建立欄位結構的語法。
        /// </summary>
        private string GetFieldsCommandText()
        {
            // 取得建立欄位結構的語法
            var sb = new StringBuilder();
            foreach (DbField field in this.DbTable.Fields)
            {
                // 取得欄位結構的命令語法
                string text = GetFieldCommandText(field);
                if (StrFunc.IsNotEmpty(text))
                {
                    if (sb.Length > 0)
                        sb.Append(",\r\n");
                    sb.Append("  " + text);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 取得單一欄位結構的命令語法。
        /// </summary>
        /// <param name="field">欄位結構。</param>
        private string GetFieldCommandText(DbField field)
        {
            string sDbType;  // 欄位型別
            string sAllowNull;  // 是否允許 Null
            string sDefault;  // 預設值
            string sDefaultValue;

            // 欄位型別
            sDbType = ConverDbType(field);
            // 是否允許 Null
            sAllowNull = field.AllowNull ? "NULL" : "NOT NULL";
            // 預設值
            sDefaultValue = GetDefaultValue(field);
            if (StrFunc.IsNotEmpty(sDefaultValue))
                sDefault = $"DEFAULT ({sDefaultValue})";
            else
                sDefault = string.Empty;

            if (StrFunc.IsEmpty(sDefault))
                return $"[{field.FieldName}] {sDbType} {sAllowNull}";
            else
                return $"[{field.FieldName}] {sDbType} {sAllowNull} {sDefault}";
        }

        /// <summary>
        /// 轉換為 SQL Server 資料庫的欄位型別。
        /// </summary>
        /// <param name="field">欄位結構。</param>
        private string ConverDbType(DbField field)
        {
            switch (field.DbType)
            {
                case FieldDbType.String:
                    return $"[nvarchar]({field.Length})";
                case FieldDbType.Text:
                    return "[nvarchar](max)";
                case FieldDbType.Boolean:
                    return "[bit]";
                case FieldDbType.Identity:
                    return "[int] IDENTITY(1,1)";
                case FieldDbType.Integer:
                    return "[int]";
                case FieldDbType.Double:
                    return "[float]";
                case FieldDbType.Currency:
                    return "[money]";
                case FieldDbType.Date:
                    return "[date]";
                case FieldDbType.DateTime:
                    return "[datetime]";
                case FieldDbType.Guid:
                    return "[uniqueidentifier]";
                case FieldDbType.Binary:
                    return "[varbinary](max)";
                default:
                    throw new InvalidOperationException($"DbType={field.DbType} is not supported");
            }
        }

        /// <summary>
        /// 取得欄位預設值。
        /// </summary>
        /// <param name="dbField">欄位結構。</param>
        private string GetDefaultValue(DbField dbField)
        {
            if (dbField.AllowNull)
                return string.Empty;
            else
                return GetDefaultValue(dbField.DbType, dbField.DefaultValue);
        }

        /// <summary>
        /// 取得欄位預設值。
        /// </summary>
        /// <param name="dbType">欄位資料型別。</param>
        /// <param name="defaultValue">預設值。</param>
        private string GetDefaultValue(FieldDbType dbType, string defaultValue)
        {
            string originalDefaultValue = DbFunc.GetSqlDefaultValue(dbType);

            switch (dbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return StrFunc.Format("N'{0}'", StrFunc.IsEmpty(defaultValue) ? originalDefaultValue : defaultValue);
                case FieldDbType.Identity:
                    return string.Empty;
                default:
                    return StrFunc.IsEmpty(defaultValue) ? originalDefaultValue : defaultValue;
            }
        }

        /// <summary>
        /// 取得主索引鍵的的命令文字。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        private string GetPrimaryKeyCommandText(string tableName)
        {
            var index = this.DbTable.GetPrimaryKey();
            if (index == null) { return string.Empty; }

            // 索引欄位
            string fields = string.Empty;
            foreach (IndexField field in index.IndexFields)
            {
                if (StrFunc.IsNotEmpty(fields))
                    fields += ", ";
                fields += $"[{field.FieldName}] {field.SortDirection.ToString().ToUpper()}";
            }

            string name = StrFunc.Format(index.Name, tableName);
            return $"CONSTRAINT [{name}] PRIMARY KEY ({fields})";
        }

        /// <summary>
        /// 取得建立索引的命令語法。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        private string GetIndexsCommandText(string tableName)
        {
            var sb = new StringBuilder();
            foreach (DbTableIndex index in this.DbTable.Indexes)
            {
                if (!index.PrimaryKey)
                    sb.AppendLine(GetIndexCommandText(tableName, index));
            }
            return sb.ToString().Trim(); // 避免最後多餘的換行
        }

        /// <summary>
        /// 取得索引的命令文字。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        /// <param name="index">資料表索引。</param>
        private string GetIndexCommandText(string tableName, DbTableIndex index)
        {
            // 索引名稱
            string name = StrFunc.Format(index.Name, tableName);
            // 索引欄位
            string fields = string.Empty;
            foreach (IndexField field in index.IndexFields)
            {
                if (StrFunc.IsNotEmpty(fields))
                    fields += ", ";
                fields += $"[{field.FieldName}] {field.SortDirection.ToString().ToUpper()}";
            }
            // 產生建立索引的語法
            if (index.Unique)
                return $"CREATE UNIQUE INDEX [{name}] ON [{tableName}] ({fields});";
            else
                return $"CREATE INDEX [{name}] ON [{tableName}] ({fields});";
        }
    }
}
