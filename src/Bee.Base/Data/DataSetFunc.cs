using System;
using System.Data;

namespace Bee.Base.Data
{
    /// <summary>
    /// Utility library for DataSet-related operations.
    /// </summary>
    public static class DataSetFunc
    {
        /// <summary>
        /// Creates a new <see cref="DataSet"/> with the specified name.
        /// </summary>
        /// <param name="datasetName">The name of the DataSet.</param>
        public static DataSet CreateDataSet(string datasetName)
        {
            var dataSet = new DataSet(datasetName);
#pragma warning disable SYSLIB0038
            dataSet.RemotingFormat = SerializationFormat.Binary;
#pragma warning restore SYSLIB0038
            return dataSet;
        }

        /// <summary>
        /// Creates a new <see cref="DataSet"/> with the default name.
        /// </summary>
        public static DataSet CreateDataSet()
        {
            return CreateDataSet("DataSet");
        }

        /// <summary>
        /// Creates a new <see cref="DataTable"/> with the specified name.
        /// </summary>
        /// <param name="tableName">The name of the DataTable.</param>
        public static DataTable CreateDataTable(string tableName)
        {
            var table = new DataTable(tableName);
#pragma warning disable SYSLIB0038
            table.RemotingFormat = SerializationFormat.Binary;
#pragma warning restore SYSLIB0038
            return table;
        }

        /// <summary>
        /// Creates a new <see cref="DataTable"/> with the default name.
        /// </summary>
        public static DataTable CreateDataTable()
        {
            return CreateDataTable("DataTable");
        }

        /// <summary>
        /// Copies the source table, retaining only the specified columns.
        /// </summary>
        /// <param name="source">The source table.</param>
        /// <param name="fieldNames">Array of column names to retain.</param>
        public static DataTable CopyDataTable(DataTable source, string[] fieldNames)
        {
            // Copy source data and schema
            var table = source.Copy();
            // Normalize field names to uppercase
            for (int N1 = 0; N1 < fieldNames.Length; N1++)
                fieldNames[N1] = StrFunc.ToUpper(fieldNames[N1]);
            // Remove columns that are not in the keep list
            for (int N1 = table.Columns.Count - 1; N1 >= 0; N1--)
            {
                string fieldName = table.Columns[N1].ColumnName.ToUpper();
                if (Array.IndexOf(fieldNames, fieldName) == -1)
                    table.Columns.Remove(fieldName);
            }
            // Restore column ordinal order to match fieldNames
            for (int N1 = 0; N1 < fieldNames.Length - 1; N1++)
            {
                string fieldName = fieldNames[N1];
                table.Columns[fieldName]?.SetOrdinal(N1);
            }
            // Return the processed table
            return table;
        }

        /// <summary>
        /// Converts all column names in the table to uppercase.
        /// </summary>
        /// <param name="dataTable">The target table.</param>
        public static void UpperColumnName(DataTable dataTable)
        {
            foreach (DataColumn column in dataTable.Columns)
                column.ColumnName = column.ColumnName.ToUpper();
        }

        /// <summary>
        /// Returns the default value for the specified field database type.
        /// </summary>
        /// <param name="dbType">The field database type.</param>
        public static object GetDefaultValue(FieldDbType dbType)
        {
            switch (dbType)
            {
                case FieldDbType.String:
                case FieldDbType.Text:
                    return string.Empty;
                case FieldDbType.Boolean:
                    return false;
                case FieldDbType.Integer:
                case FieldDbType.Decimal:
                case FieldDbType.Currency:
                    return 0;
                case FieldDbType.Date:
                    return DateTime.Today;
                case FieldDbType.DateTime:
                    return DateTime.Now;
                case FieldDbType.Guid:
                    return Guid.Empty;
                default:
                    return DBNull.Value;
            }
        }
    }
}
