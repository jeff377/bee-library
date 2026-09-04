using System.Data;
using System.Globalization;
using Bee.Base.Data;
using Bee.Definition.Forms;

namespace Bee.Api.Client
{
    /// <summary>
    /// Converts between the values held in a form's <see cref="DataSet"/> and the strings that
    /// UI bindings exchange, and builds the empty <see cref="DataSet"/> a form starts from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These rules belong to the wire-facing shape of a form, not to any one UI toolkit: the same
    /// <see cref="FormSchema"/> and the same server responses drive every head. They live here
    /// because <c>Bee.Api.Client</c> is the nearest assembly both the desktop and the web head
    /// already reference — a common ancestor that does not pull a web head into the desktop UI
    /// family.
    /// </para>
    /// <para>
    /// WARNING: the two heads previously kept private copies of this logic, marked "deliberately
    /// parallel" with a note that nothing enforced the parallel. They diverged:
    /// <see cref="ToColumnValue"/> was fixed on one side to stop writing <see cref="DBNull"/> into
    /// a non-nullable column and the other side kept the bug. A comment is not an enforcement
    /// mechanism; one implementation is.
    /// </para>
    /// </remarks>
    public static class FormValueBinding
    {
        /// <summary>
        /// Builds an empty <see cref="DataSet"/> shaped by <paramref name="schema"/>.
        /// </summary>
        /// <param name="schema">The form schema describing the tables and their fields.</param>
        /// <returns>A <see cref="DataSet"/> named after the schema's ProgId, with one empty table per schema table.</returns>
        public static DataSet BuildEmptyDataSet(FormSchema schema)
        {
            ArgumentNullException.ThrowIfNull(schema);

            var dataSet = new DataSet(schema.ProgId);

            if (schema.Tables is null)
                return dataSet;

            var masterTable = schema.MasterTable;
            foreach (var table in schema.Tables)
            {
                var dataTable = new DataTable(table.TableName);
                if (table.Fields is not null)
                {
                    foreach (var field in table.Fields)
                        dataTable.AddColumn(field.FieldName, field.DbType);
                }
                dataSet.Tables.Add(dataTable);
            }

            if (masterTable is not null && !dataSet.Tables.Contains(masterTable.TableName))
            {
                var dataTable = new DataTable(masterTable.TableName);
                dataSet.Tables.Add(dataTable);
            }

            return dataSet;
        }

        /// <summary>
        /// Renders a cell value as the string a UI binding displays and edits.
        /// </summary>
        /// <param name="raw">The cell value, which may be <c>null</c> or <see cref="DBNull"/>.</param>
        /// <returns>The display string; empty for a null or <see cref="DBNull"/> value.</returns>
        public static string ToBindingString(object? raw)
        {
            if (raw is null || raw == DBNull.Value)
                return string.Empty;

            return raw switch
            {
                // ISO 8601 keeps round-trip parity with the date editors on every head:
                // desktop DatePicker / TextBox controls and HTML date/datetime-local inputs
                // read and write the same shape.
                DateTime dt => dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => raw.ToString() ?? string.Empty,
            };
        }

        /// <summary>
        /// Coerces an edited string back into a value the column will accept.
        /// </summary>
        /// <param name="value">The edited string, which may be <c>null</c> or empty.</param>
        /// <param name="column">The target column, whose <see cref="DataColumn.DataType"/> drives the conversion.</param>
        /// <returns>A value assignable to <paramref name="column"/>; never <c>null</c>.</returns>
        public static object ToColumnValue(string? value, DataColumn column)
        {
            ArgumentNullException.ThrowIfNull(column);

            if (string.IsNullOrEmpty(value))
            {
                if (column.AllowDBNull) return DBNull.Value;

                // Non-nullable column: prefer the column's own DefaultValue when it
                // was properly seeded (DataTableExtensions.AddColumn pins this to a
                // type-appropriate non-null for every FieldDbType). Server-side
                // responses often arrive with raw ADO.NET columns whose DefaultValue
                // is still DBNull — for those, synthesise a non-null fallback from
                // the column's CLR type rather than writing DBNull into a NOT NULL
                // column, which would raise NoNullAllowedException on EndEdit.
                if (column.DefaultValue is not null && column.DefaultValue != DBNull.Value)
                    return column.DefaultValue;
                return GetEmptyValue(column.DataType);
            }

            var targetType = column.DataType;
            if (targetType == typeof(string))
                return value;
            if (targetType == typeof(Guid))
                return Guid.Parse(value);
            if (targetType == typeof(byte[]))
                return Convert.FromBase64String(value);
            if (targetType == typeof(DateTime))
                return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the non-null value that stands for "empty" for <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType">The column's CLR type.</param>
        /// <returns>The empty value, or <see cref="DBNull.Value"/> for a reference type with no natural empty.</returns>
        public static object GetEmptyValue(Type targetType)
        {
            ArgumentNullException.ThrowIfNull(targetType);

            if (targetType == typeof(string)) return string.Empty;
            if (targetType == typeof(Guid)) return Guid.Empty;
            if (targetType == typeof(DateTime)) return DateTime.MinValue;
            if (targetType == typeof(byte[])) return Array.Empty<byte>();
            if (targetType.IsValueType) return Activator.CreateInstance(targetType)!;
            return DBNull.Value;
        }
    }
}
