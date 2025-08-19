using System.Data;
using Bee.Base;
using Bee.Define;

namespace Bee.Db
{
    /// <summary>
    /// SQL Server 資料庫結構輔助類別。
    /// </summary>
    public class SqlDbTableHelper
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseId">資料庫編號。</param>
        public SqlDbTableHelper(string databaseId)
        {
            DatabaseId = databaseId;
        }

        #endregion

        /// <summary>
        /// 資料庫編號。
        /// </summary>
        public string DatabaseId { get; }

        /// <summary>
        /// 建立資料表結構。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        public DbTable CreateDbTable(string tableName)
        {
            // 若資料表不存在則回傳 null
            if (!TableExists(tableName)) { return null; }

            var dbTable = new DbTable();
            dbTable.TableName = tableName;

            // 取得索引的資料來源
            var indexes = GetTableIndexes(tableName);
            // 解析主索引鍵
            ParsePrimaryKey(dbTable, indexes);
            // 解析索引集合
            ParseIndexes(dbTable, indexes);

            // 取得欄位結構的資料來源
            var columns = GetColumns(tableName);
            foreach (DataRow row in columns.Rows)
            {
                var dbField = ParseDbField(row);
                dbTable.Fields.Add(dbField);
            }

            return dbTable;
        }

        /// <summary>
        /// 判斷是否存在指定資料表。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        private bool TableExists(string tableName)
        {
            var helper = DbFunc.CreateDbCommandHelper();
            helper.AddParameter("TableName", FieldDbType.String, tableName);
            string sSQL = "Select Count(*) From sys.tables A Where A.name={0}";
            helper.SetCommandFormatText(sSQL);
            int iCount = BaseFunc.CInt(SysDb.ExecuteScalar(BackendInfo.DatabaseId, helper.DbCommand));
            return (iCount > 0) ? true : false;
        }

        /// <summary>
        /// 取得指定資料表索引。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        private DataTable GetTableIndexes(string tableName)
        {
            IDbCommandHelper oHelper;
            string sSQL;
            DataTable oTable;

            oHelper = DbFunc.CreateDbCommandHelper();
            oHelper.AddParameter("TableName", FieldDbType.String, tableName);
            sSQL = "SELECT A.name as FieldName,D.is_primary_key as IsPrimaryKey,D.is_unique as IsUnique,D.name,\n" +
                          "C.key_ordinal as KeyOrdinal,C.is_descending_key As IsDesc \n" +
                          "FROM sys.columns A  \n" +
                          "INNER JOIN sys.tables B On A.object_id=B.object_id \n" +
                          "INNER JOIN sys.index_columns C On A.object_id=C.object_id And A.column_id=C.column_id \n" +
                          "LEFT JOIN sys.indexes D On A.object_id=D.object_id And C.index_id=D.index_id \n" +
                          "WHERE B.name={0} \n" +
                          "Order By D.is_primary_key,C.key_ordinal";
            oHelper.SetCommandFormatText(sSQL);
            oTable = SysDb.ExecuteDataTable(BackendInfo.DatabaseId, oHelper.DbCommand);
            oTable.TableName = "TableIndex";
            return oTable;
        }

        /// <summary>
        /// 解析主索引鍵。
        /// </summary>
        /// <param name="dbTable">資料表結構。</param>
        /// <param name="table">索引資料表。</param>
        private void ParsePrimaryKey(DbTable dbTable, DataTable table)
        {
            DbTableIndex oTableIndex;
            IndexField oIndexField;

            if (DataSetFunc.IsEmpty(table)) { return; }

            table.DefaultView.RowFilter = "IsPrimaryKey=true";
            table.DefaultView.Sort = "KeyOrdinal";
            if (DataSetFunc.IsEmpty(table.DefaultView)) { return; }

            // 取得索引名稱
            string name = BaseFunc.CStr(table.DefaultView[0]["name"]);
            // 取得主索引
            oTableIndex = new DbTableIndex();
            oTableIndex.PrimaryKey = true;
            oTableIndex.Name = name;
            oTableIndex.Unique = true;
            dbTable.Indexes.Add(oTableIndex);
            foreach (DataRowView row in table.DefaultView)
            {
                oIndexField = new IndexField();
                oIndexField.FieldName = BaseFunc.CStr(row["FieldName"]);
                oIndexField.SortDirection = BaseFunc.CBool(row["IsDesc"]) ? SortDirection.Desc : SortDirection.Asc;
                oTableIndex.IndexFields.Add(oIndexField);
            }
            // 刪除已處理的主索引鍵資料
            DataSetFunc.DeleteRows(table.DefaultView, true);
        }

        /// <summary>
        /// 解析索引集合。
        /// </summary>
        /// <param name="dbTable">資料表結構。</param>
        /// <param name="table">索引資料表。</param>
        private void ParseIndexes(DbTable dbTable, DataTable table)
        {
            DbTableIndex oTableIndex;
            IndexField oIndexField;
            DataRow oRow;
            string sName;
            bool bUnique;

            while (!DataSetFunc.IsEmpty(table))
            {
                oRow = table.Rows[0];
                sName = BaseFunc.CStr(oRow["Name"]);  // 取得索引名稱
                bUnique = BaseFunc.CBool(oRow["IsUnique"]);

                oTableIndex = new DbTableIndex();
                oTableIndex.Name = sName;
                oTableIndex.Unique = bUnique;
                dbTable.Indexes.Add(oTableIndex);

                table.DefaultView.RowFilter = $"Name='{sName}'";
                table.DefaultView.Sort = "Name,KeyOrdinal";
                foreach (DataRowView row in table.DefaultView)
                {
                    oIndexField = new IndexField();
                    oIndexField.FieldName = BaseFunc.CStr(row["FieldName"]);
                    oIndexField.SortDirection = BaseFunc.CBool(row["IsDesc"]) ? SortDirection.Desc : SortDirection.Asc;
                    oTableIndex.IndexFields.Add(oIndexField);
                }
                // 刪除已處理的索引鍵資料
                DataSetFunc.DeleteRows(table.DefaultView, true);
            }
        }

        /// <summary>
        /// 取得指定資料表的欄位清單。
        /// </summary>
        /// <param name="tableName">資料表名稱。</param>
        private DataTable GetColumns(string tableName)
        {
            IDbCommandHelper oHelper;
            string sSQL;
            DataTable oTable;

            oHelper = DbFunc.CreateDbCommandHelper();
            oHelper.AddParameter("TableName", FieldDbType.String, tableName);
            sSQL = "SELECT A.is_nullable as AllowDBNull,A.is_identity as AutoIncrement, \n" +
                          "IsNull(C.seed_value,0) as AutoIncrementSeed,IsNull(C.increment_value,0) as AutoIncrementStep, \n" +
                          "IsNull(E.name,'') as BindDefault,TYPE_NAME(A.system_type_id) as DbType,A.precision,A.scale as Decimals, \n" +
                          "IsNull(F.definition,'') as DefaultValue,A.name as FieldName,A.max_length as Length, \n" +
                          "IsNull((select value from fn_listextendedproperty(NULL, 'user', 'dbo', 'table', B.name, 'column', A.name)),'') as Description \n" +
                          "FROM sys.columns A \n" +
                          "INNER JOIN sys.tables B On A.object_id=B.object_id \n" +
                          "LEFT JOIN sys.identity_columns C on A.object_id=C.object_id and A.column_id=C.column_id \n" +
                          "LEFT JOIN sys.objects E on E.object_id = A.default_object_id and E.type = 'D' \n" +
                          "LEFT JOIN sys.default_constraints F on F.parent_object_id = A.object_id and F.parent_column_id = A.column_id \n" +
                          "WHERE B.name={0} \n" +
                          "ORDER BY A.column_id";
            oHelper.SetCommandFormatText(sSQL);
            oTable = SysDb.ExecuteDataTable(BackendInfo.DatabaseId, oHelper.DbCommand);
            oTable.TableName = "Columns";
            return oTable;
        }

        /// <summary>
        /// 建立欄位結構。
        /// </summary>
        /// <param name="row">欄位資訊的資料列。</param>
        private DbField ParseDbField(DataRow row)
        {
            var dbField = new DbField();
            dbField.FieldName = BaseFunc.CStr(row["FieldName"]);
            dbField.Caption = BaseFunc.CStr(row["Description"]);
            dbField.AllowNull = BaseFunc.CBool(row["AllowDBNull"]);

            if (BaseFunc.CBool(row["AutoIncrement"]))
                dbField.DbType = FieldDbType.Identity;
            else
                dbField.DbType = GetFieldDbType(BaseFunc.CStr(row["DbType"]), BaseFunc.CInt(row["precision"]), BaseFunc.CInt(row["Length"]));

            if (dbField.DbType == FieldDbType.String)
            {
                if (StrFunc.ToUpper(BaseFunc.CStr(row["DbType"])) == "NVARCHAR")
                    dbField.Length = BaseFunc.CInt(row["Length"]) / 2;
                else
                    dbField.Length = BaseFunc.CInt(row["Length"]);
            }

            dbField.DefaultValue = this.ParseDBDefaultValue(BaseFunc.CStr(row["DbType"]), BaseFunc.CStr(row["DefaultValue"]));
            return dbField;
        }

        /// <summary>
        /// 將資料型別轉型為 EFieldDbType 列舉型別。
        /// </summary>
        /// <param name="dataType">資料型別。</param>
        /// <param name="dataPrecision">精確度。</param>
        /// <param name="length">資料長度。</param>
        public static FieldDbType GetFieldDbType(string dataType, int dataPrecision, int length)
        {
            switch (StrFunc.ToUpper(dataType))
            {
                case "NCHAR":
                    return FieldDbType.String;
                case "NVARCHAR":
                    if (length == -1)
                        return FieldDbType.Text;
                    else
                        return FieldDbType.String;
                case "BIT":
                    return FieldDbType.Boolean;
                case "INT":
                    return FieldDbType.Integer;
                case "FLOAT":
                    return FieldDbType.Double;
                case "MONEY":
                    return FieldDbType.Currency;
                case "DATE":
                    return FieldDbType.Date;
                case "DATETIME":
                    return FieldDbType.DateTime;
                case "UNIQUEIDENTIFIER":
                    return FieldDbType.Guid;
                case "VARBINARY":
                    return FieldDbType.Binary;
                default:
                    return FieldDbType.String;
            }
        }

        /// <summary>
        /// 解析從資料庫取回的欄位預設值，如果跟程式內定的預設值相同，則回傳空白。
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string ParseDBDefaultValue(string dataType, string defaultValue)
        {
            FieldDbType dbType = GetFieldDbType(dataType, 0, 0);
            string originalDefaultValue = DbFunc.GetSqlDefaultValue(dbType);

            switch (StrFunc.ToUpper(dataType))
            {
                case "CHAR":
                case "VARCHAR":
                    defaultValue = StrFunc.LeftRightCut(defaultValue, "('", "')");
                    break;
                case "NCHAR":
                case "NVARCHAR":
                    defaultValue = StrFunc.LeftRightCut(defaultValue, "(N'", "')");
                    defaultValue = StrFunc.LeftRightCut(defaultValue, "('", "')");
                    break;
                case "BIT":
                case "INT":
                case "MONEY":
                case "FLOAT":
                    defaultValue = StrFunc.LeftRightCut(defaultValue, "((", "))");
                    break;
                case "DATE":
                case "DATETIME":
                case "UNIQUEIDENTIFIER":
                    defaultValue = StrFunc.LeftRightCut(defaultValue, "(", ")");
                    break;
                default:
                    defaultValue = string.Empty;
                    break;
            }

            //檢查資料庫的預設值跟程式內定的預設值是否相同。
            if (StrFunc.Equals(originalDefaultValue, defaultValue))
                return string.Empty;
            else
                return defaultValue;
        }

    }
}
