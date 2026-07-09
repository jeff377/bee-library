using System.Data;

namespace Bee.Base.Data
{
    /// <summary>
    /// Extension methods for <see cref="DataTable"/>.
    /// </summary>
    public static class DataTableExtensions
    {
        /// <summary>
        /// Creates a column with the specified settings and adds it to the table.
        /// </summary>
        /// <param name="table">The target table.</param>
        /// <param name="fieldName">The column name.</param>
        /// <param name="caption">The column caption.</param>
        /// <param name="dataType">The data type of the column.</param>
        /// <param name="defaultValue">The default value for the column.</param>
        /// <param name="dateTimeMode">The <see cref="DataSetDateTime"/> mode for DateTime columns.</param>
        private static DataColumn AddColumn(this DataTable table, string fieldName, string caption, Type dataType, object defaultValue, DataSetDateTime dateTimeMode = DataSetDateTime.Unspecified)
        {
            // Column names are canonicalized to lowercase so the in-memory DataSet matches the
            // lowercase field name used at every other layer (database, FormField, expressions, UI) —
            // see ADR-029. Invariant lowercasing keeps ASCII snake_case identifiers stable and avoids
            // the Turkish-I hazard of a culture-aware fold.
            var column = new DataColumn(fieldName.ToLowerInvariant(), dataType);
            column.DefaultValue = defaultValue;

            if (dataType == typeof(DateTime))
                column.DateTimeMode = dateTimeMode;

            if (!ValueUtilities.IsNullOrDBNull(defaultValue))
                column.AllowDBNull = false;

            if (StringUtilities.IsNotEmpty(caption))
                column.Caption = caption;

            table.Columns.Add(column);
            return column;
        }

        /// <summary>
        /// Creates a column with the specified type and default value and adds it to the table.
        /// </summary>
        /// <param name="table">The target table.</param>
        /// <param name="fieldName">The column name.</param>
        /// <param name="dataType">The data type of the column.</param>
        /// <param name="defaultValue">The default value for the column.</param>
        private static DataColumn AddColumn(this DataTable table, string fieldName, Type dataType, object defaultValue)
        {
            return AddColumn(table, fieldName, string.Empty, dataType, defaultValue);
        }

        /// <summary>
        /// Creates a column for the specified field database type and adds it to the table.
        /// </summary>
        /// <param name="table">The target table.</param>
        /// <param name="fieldName">The column name.</param>
        /// <param name="dbType">The field database type.</param>
        public static DataColumn AddColumn(this DataTable table, string fieldName, FieldDbType dbType)
        {
            var dataType = DbTypeConverter.ToType(dbType);
            object defaultValue = dbType.GetDefaultValue();
            return AddColumn(table, fieldName, dataType, defaultValue);
        }

        /// <summary>
        /// Creates a column for the specified field database type with an explicit default value and adds it to the table.
        /// </summary>
        /// <param name="table">The target table.</param>
        /// <param name="fieldName">The column name.</param>
        /// <param name="dbType">The field database type.</param>
        /// <param name="defaultValue">The default value for the column.</param>
        public static DataColumn AddColumn(this DataTable table, string fieldName, FieldDbType dbType, object defaultValue)
        {
            var dataType = DbTypeConverter.ToType(dbType);
            return AddColumn(table, fieldName, dataType, defaultValue);
        }

        /// <summary>
        /// Creates a column with a caption for the specified field database type and adds it to the table.
        /// </summary>
        /// <param name="table">The target table.</param>
        /// <param name="fieldName">The column name.</param>
        /// <param name="caption">The column caption.</param>
        /// <param name="dbType">The field database type.</param>
        /// <param name="defaultValue">The default value for the column.</param>
        public static DataColumn AddColumn(this DataTable table, string fieldName, string caption, FieldDbType dbType, object defaultValue)
        {
            var dataType = DbTypeConverter.ToType(dbType);
            return AddColumn(table, fieldName, caption, dataType, defaultValue);
        }

        /// <summary>
        /// Determines whether the table contains the specified column.
        /// </summary>
        /// <param name="dataTable">The target table.</param>
        /// <param name="fieldName">The column name to check.</param>
        public static bool HasField(this DataTable dataTable, string fieldName)
        {
            return dataTable.Columns.Contains(fieldName);
        }

        /// <summary>
        /// Sets the primary key of the table using a comma-separated list of column names.
        /// </summary>
        /// <param name="table">The target table.</param>
        /// <param name="fieldNames">A comma-separated string of column names that form the primary key.</param>
        public static void SetPrimaryKey(this DataTable table, string fieldNames)
        {
            string[] fieldNameArray = StringUtilities.Split(fieldNames, ",");
            var dataColumns = new DataColumn[fieldNameArray.Length];
            int iIndex = 0;
            foreach (string fieldName in fieldNameArray)
            {
                dataColumns[iIndex] = table.Columns[fieldName]!;
                iIndex++;
            }
            table.PrimaryKey = dataColumns;
        }

        /// <summary>
        /// Determines whether the table contains no rows.
        /// </summary>
        /// <param name="dataTable">The target table.</param>
        public static bool IsEmpty(this DataTable dataTable)
        {
            // A null table or a table with zero rows is considered empty
            return dataTable == null || (dataTable.Rows.Count == 0);
        }

        /// <summary>
        /// Canonicalizes all column names in the table to lowercase, so a table read from any database
        /// provider (Oracle returns uppercase, PostgreSQL lowercase, …) exposes the single lowercase
        /// field name used at every other layer — see ADR-029. Invariant lowercasing avoids the
        /// Turkish-I hazard of a culture-aware fold on ASCII snake_case identifiers.
        /// </summary>
        /// <param name="dataTable">The target table.</param>
        public static void LowercaseColumnNames(this DataTable dataTable)
        {
            foreach (DataColumn column in dataTable.Columns)
                column.ColumnName = column.ColumnName.ToLowerInvariant();
        }
    }
}
