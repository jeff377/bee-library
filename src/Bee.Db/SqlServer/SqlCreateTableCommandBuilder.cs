using System;
using System.Text;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// SQL Server 資料庫建立資料表命令語法產生器。
    /// </summary>
    public class SqlCreateTableCommandBuilder : ICreateTableCommandBuilder
    {
        private DbTable _dbTable = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public SqlCreateTableCommandBuilder()
        { }

        #endregion

        /// <summary>
        /// 資料表結構。
        /// </summary>
        private DbTable DbTable
        {
            get { return _dbTable; }
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
            _dbTable = dbTable;

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
            var sb = new StringBuilder();
            string tmpTableName = $"tmp_{this.TableName}";
            // 刪除暫存資料表
            string sql = GetDropTableCommandText(tmpTableName);
            sb.AppendLine("-- 刪除暫存資料表");
            sb.AppendLine(sql);
            // 建立暫存資料表
            sql = GetCreateTableCommandText(tmpTableName);
            sb.AppendLine("-- 建立暫存資料表");
            sb.AppendLine(sql);
            // 搬移資料
            sql = GetInsertTableCommandText(this.TableName, tmpTableName);
            sb.AppendLine("-- 搬移資料");
            sb.AppendLine(sql);
            // 刪除舊資料表
            sql = GetDropTableCommandText(this.TableName);
            sb.AppendLine("-- 刪除舊資料表");
            sb.AppendLine(sql);
            // 暫存資料表更名
            sb.AppendLine("-- 暫存資料表更名");
            sql = GetRenameTableCommandText(tmpTableName, this.TableName);
            sb.AppendLine(sql);

            return sb.ToString();
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
           // 取得要搬除的欄位清單
            string fields = string.Empty;
            foreach (DbField field in this.DbTable.Fields)
            {
                if (field.UpgradeAction != DbUpgradeAction.New && field.DbType != FieldDbType.AutoIncrement)
                {
                    if (StrFunc.IsNotEmpty(fields))
                        fields += ", ";
                    fields += $"[{field.FieldName}]";
                }
            }
            // 組成  INSERT INTO SELECT 語法
            string  sql = $"INSERT INTO [{newTableName}] ({fields}) \n" +
                                  $"SELECT {fields} FROM [{tableName}];";
            return sql;
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
            // 資料表名稱
            string dbTableName = StrFunc.IsNotEmpty(tableName) ? tableName : this.DbTable.TableName;
            // 取得建立欄位結構的語法
            string fields = GetFieldsCommandText();
            // 取得建立主索引的命令語法
            string primaryKey = GetPrimaryKeyCommandText(dbTableName);
            // 取得建立索引的命令語法
            string indexs = GetIndexsCommandText(dbTableName);

            var sb = new StringBuilder();
            // 組成 Create Table 的語法
            sb.Append($"CREATE TABLE [{dbTableName}] (\r\n{fields}");
            if (StrFunc.IsNotEmpty(primaryKey))
                sb.Append($",\r\n  {primaryKey}");
            sb.Append("\r\n);");
            // 加上建立索引語法
            if (StrFunc.IsNotEmpty(indexs))
                sb.Append($"\r\n{indexs}");
            return sb.ToString();
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
            // 欄位型別
            string dbType = ConverDbType(field);
            // 是否允許 Null
            string allowNull = field.AllowNull ? "NULL" : "NOT NULL";
            // 預設值
            string defaultValue = GetDefaultValue(field);
            string defaultText;
            if (StrFunc.IsNotEmpty(defaultValue))
                defaultText = $"DEFAULT ({defaultValue})";
            else
                defaultText = string.Empty;

            if (StrFunc.IsEmpty(defaultText))
                return $"[{field.FieldName}] {dbType} {allowNull}";
            else
                return $"[{field.FieldName}] {dbType} {allowNull} {defaultText}";
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
                case FieldDbType.AutoIncrement:
                    return "[int] IDENTITY(1,1)";
                case FieldDbType.Short:
                    return "[smallint]";
                case FieldDbType.Integer:
                    return "[int]";
                case FieldDbType.Long:
                    return "[bigint]";
                case FieldDbType.Decimal:
                    {
                        int precision = field.Precision > 0 ? field.Precision : 18;
                        int scale = field.Scale > 0 ? field.Scale : 0;
                        return $"[decimal]({precision},{scale})";
                    }
                case FieldDbType.Currency:
                    return "[decimal](19,4)";
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
                case FieldDbType.AutoIncrement:
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
