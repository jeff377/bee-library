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
        /// Creates a column for the specified field database type and adds it to the table.
        /// </summary>
        /// <param name="table">The target table.</param>
        /// <param name="fieldName">The column name.</param>
        /// <param name="dbType">The field database type.</param>
        public static DataColumn AddColumn(this DataTable table, string fieldName, FieldDbType dbType)
        {
            return AddColumn(table, fieldName, string.Empty, dbType, dbType.GetDefaultValue());
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
            return AddColumn(table, fieldName, string.Empty, dbType, defaultValue);
        }

        /// <summary>
        /// Creates a column with a caption for the specified field database type and adds it to the table.
        /// </summary>
        /// <param name="table">The target table.</param>
        /// <param name="fieldName">The column name.</param>
        /// <param name="caption">The column caption.</param>
        /// <param name="dbType">The field database type.</param>
        /// <param name="defaultValue">The default value for the column.</param>
        /// <remarks>
        /// All <see cref="FieldDbType"/> overloads funnel through here, so the declared field type is
        /// recorded on the column in exactly one place. Without that record several
        /// <see cref="FieldDbType"/> values would be indistinguishable downstream — most importantly
        /// <see cref="FieldDbType.Date"/> and <see cref="FieldDbType.DateTime"/>, which share a CLR type.
        /// </remarks>
        public static DataColumn AddColumn(this DataTable table, string fieldName, string caption, FieldDbType dbType, object defaultValue)
        {
            var dataType = DbTypeConverter.ToType(dbType);
            var column = AddColumn(table, fieldName, caption, dataType, defaultValue);
            column.ApplyFieldDbType(dbType);
            return column;
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

        /// <summary>
        /// Records the declared <see cref="FieldDbType"/> for each named column of the table.
        /// </summary>
        /// <param name="table">The target table.</param>
        /// <param name="dbType">The declared field database type.</param>
        /// <param name="columnNames">The column names to mark.</param>
        /// <exception cref="ArgumentException">A named column does not exist in the table.</exception>
        /// <remarks>
        /// Column names are matched case-insensitively by <see cref="DataColumnCollection"/>, which suits
        /// tables passed through <see cref="LowercaseColumnNames"/>. A name matching no column raises an
        /// error rather than being skipped: silently ignoring a typo would reproduce the very
        /// "looks declared but has no effect" failure this record exists to remove.
        /// </remarks>
        public static void ApplyFieldDbType(this DataTable table, FieldDbType dbType, params string[] columnNames)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(columnNames);

            foreach (var name in columnNames)
            {
                var column = table.Columns[name]
                    ?? throw new ArgumentException(
                        $"Column '{name}' does not exist in table '{table.TableName}'.", nameof(columnNames));
                column.ApplyFieldDbType(dbType);
            }
        }

        /// <summary>
        /// Marks the named columns as calendar-day columns (<see cref="FieldDbType.Date"/>).
        /// </summary>
        /// <param name="table">The target table.</param>
        /// <param name="columnNames">The column names to mark.</param>
        /// <exception cref="ArgumentException">A named column does not exist in the table.</exception>
        /// <remarks>
        /// Call this on a <see cref="DataTable"/> produced by hand-written SQL (reports, batch jobs, any
        /// AnyCode query) so its calendar-day columns describe themselves on the wire. Queries the
        /// framework generates from a schema are marked by the framework and need no call here.
        /// </remarks>
        public static void SetDateColumns(this DataTable table, params string[] columnNames)
        {
            table.ApplyFieldDbType(FieldDbType.Date, columnNames);
        }

        /// <summary>
        /// Forces every <see cref="DateTime"/> column of the table to
        /// <see cref="DataSetDateTime.Unspecified"/>, leaving the stored values untouched.
        /// </summary>
        /// <param name="table">The target table.</param>
        /// <remarks>
        /// Tables this framework builds through <c>AddColumn</c> already use
        /// <see cref="DataSetDateTime.Unspecified"/>. Tables that ADO.NET builds for us do not:
        /// <c>DbDataAdapter.Fill</c>, <c>DataTable.Load</c> and <c>DataSet.ReadXml</c> all leave the
        /// .NET default of <see cref="DataSetDateTime.UnspecifiedLocal"/> in place. That default is
        /// the one mode that writes a time-zone offset into XML, so a table read from the database
        /// and then persisted as XML (the audit DiffGram does exactly this) carries an offset that a
        /// reader in another zone applies on the way back in — shifting the value, possibly across a
        /// day boundary. MessagePack and JSON are unaffected either way. See
        /// docs/adr/adr-032-datetime-timezone.md.
        ///
        /// Only <see cref="DataSetDateTime.UnspecifiedLocal"/> columns are converted. The
        /// <c>Utc</c> and <c>Local</c> modes carry a deliberate declaration and cannot be switched
        /// once a column holds data, so they are left alone rather than made to throw.
        /// </remarks>
        public static void NormalizeDateTimeMode(this DataTable table)
        {
            ArgumentNullException.ThrowIfNull(table);

            foreach (DataColumn column in table.Columns)
            {
                if (column.DataType == typeof(DateTime) &&
                    column.DateTimeMode == DataSetDateTime.UnspecifiedLocal)
                {
                    column.DateTimeMode = DataSetDateTime.Unspecified;
                }
            }
        }
    }
}
