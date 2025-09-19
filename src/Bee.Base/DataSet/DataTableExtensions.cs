using System;
using System.Data;

namespace Bee.Base
{
    /// <summary>
    /// DataTable 的擴充方法。
    /// </summary>
    public static class DataTableExtensions
    {
        /// <summary>
        /// 建立欄位並加入資料表中。
        /// </summary>
        /// <param name="table">資料表。</param>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">欄位標題。</param>
        /// <param name="dataType">資料型別。</param>
        /// <param name="defaultValue">預設值。</param>
        /// <param name="dateTimeMode">設定資料行的 DateTimeMode。</param>
        private static DataColumn AddColumn(this DataTable table, string fieldName, string caption, Type dataType, object defaultValue, DataSetDateTime dateTimeMode = DataSetDateTime.Unspecified)
        {
            // 欄位名稱全轉為大寫
            var column = new DataColumn(fieldName.ToUpper(), dataType);
            column.DefaultValue = defaultValue;

            if (dataType == typeof(DateTime))
                column.DateTimeMode = dateTimeMode;

            if (!BaseFunc.IsNullOrDBNull(defaultValue))
                column.AllowDBNull = false;

            if (StrFunc.IsNotEmpty(caption))
                column.Caption = caption;

            table.Columns.Add(column);
            return column;
        }

        /// <summary>
        /// 建立欄位並加入資料表中。
        /// </summary>
        /// <param name="table">資料表。</param>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="dataType">資料型別。</param>
        /// <param name="defaultValue">預設值。</param>
        private static DataColumn AddColumn(this DataTable table, string fieldName, Type dataType, object defaultValue)
        {
            return AddColumn(table, fieldName, string.Empty, dataType, defaultValue);
        }

        /// <summary>
        /// 建立欄位並加入資料表。
        /// </summary>
        /// <param name="table">資料表。</param>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="dbType">欄位資料型別。</param>
        public static DataColumn AddColumn(this DataTable table, string fieldName, FieldDbType dbType)
        {
            var dataType = DbTypeConverter.ToType(dbType);
            object defaultValue = DataSetFunc.GetDefaultValue(dbType);
            return AddColumn(table, fieldName, dataType, defaultValue);
        }

        /// <summary>
        /// 建立欄位並加入資料表。
        /// </summary>
        /// <param name="table">資料表。</param>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="dbType">欄位資料型別。</param>
        /// <param name="defaultValue">預設值。</param>
        public static DataColumn AddColumn(this DataTable table, string fieldName, FieldDbType dbType, object defaultValue)
        {
            var dataType = DbTypeConverter.ToType(dbType);
            return AddColumn(table, fieldName, dataType, defaultValue);
        }

        /// <summary>
        /// 建立欄位並加入資料表中。
        /// </summary>
        /// <param name="table">資料表。</param>
        /// <param name="fieldName">欄位名稱。</param>
        /// <param name="caption">欄位標題。</param>
        /// <param name="dbType">欄位資料型別。</param>
        /// <param name="defaultValue">預設值。</param>
        public static DataColumn AddColumn(this DataTable table, string fieldName, string caption, FieldDbType dbType, object defaultValue)
        {
            var dataType = DbTypeConverter.ToType(dbType);
            return AddColumn(table, fieldName, caption, dataType, defaultValue);
        }

        /// <summary>
        /// 判斷是否有指定欄位。
        /// </summary>
        /// <param name="dataTable">資料表。</param>
        /// <param name="fieldName">欄位名稱。</param>
        public static bool HasField(this DataTable dataTable, string fieldName)
        {
            return dataTable.Columns.Contains(fieldName);
        }

        /// <summary>
        /// 設定資料表的主索引鍵。
        /// </summary>
        /// <param name="table">資料表。</param>
        /// <param name="fieldNames">主索引鍵的欄位集合字串，以逗點分隔多個欄位。</param>
        public static void SetPrimaryKey(this DataTable table, string fieldNames)
        {
            string[] fieldNameArray = StrFunc.Split(fieldNames, ",");
            var dataColumns = new DataColumn[fieldNameArray.Length];
            int iIndex = 0;
            foreach (string fieldName in fieldNameArray)
            {
                dataColumns[iIndex] = table.Columns[fieldName];
                iIndex++;
            }
            table.PrimaryKey = dataColumns;
        }

        /// <summary>
        /// 判斷資料表是否無資料。
        /// </summary>
        /// <param name="dataTable">資料表。</param>
        public static bool IsEmpty(this DataTable dataTable)
        {
            // 資料表為 Null 或資料列數為零，皆視為無資料
            return dataTable == null || (dataTable.Rows.Count == 0);
        }
    }
}
