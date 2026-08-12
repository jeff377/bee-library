using System.Data;
using System.Globalization;
using Bee.Base;
using Bee.Base.Data;
using Bee.Definition.Database;
using Bee.Definition.Forms;

namespace Bee.Repository.Form
{
    /// <summary>
    /// Building the empty `DataSet` a form schema describes, and the table order it implies.
    /// </summary>
    /// <remarks>
    /// Pure construction from the schema: no command runs and no connection opens here. The
    /// master-first ordering matters to callers that write — a detail row inserted before its
    /// master violates the foreign key.
    /// </remarks>
    public partial class DataFormRepository
    {
        private IEnumerable<FormTable> EnumerateDetailTables()
        {
            if (_schema.Tables == null)
                yield break;

            foreach (FormTable table in _schema.Tables)
            {
                if (string.Equals(table.TableName, ProgId, StringComparison.OrdinalIgnoreCase))
                    continue;
                yield return table;
            }
        }

        /// <summary>
        /// Enumerates the form's tables master-first, then each detail. Save applies them in this
        /// order so a newly inserted master row exists before the detail rows that reference it.
        /// </summary>
        private IEnumerable<FormTable> EnumerateTablesMasterFirst(FormTable masterTable)
        {
            yield return masterTable;
            foreach (var detail in EnumerateDetailTables())
                yield return detail;
        }

        /// <summary>
        /// Replays the schema's declared field types over a table read from the database.
        /// </summary>
        /// <param name="table">The table returned by the query; null passes through.</param>
        /// <param name="formTable">The form table describing the query shape.</param>
        /// <remarks>
        /// A provider reports a calendar-day column as `DateTime`, indistinguishable from an instant.
        /// Marking here keeps a table read from SQL describing itself the same way as one built from
        /// the schema by <see cref="BuildEmptyDataTable"/>, whose `AddColumn` calls mark as they build.
        /// </remarks>
        private static DataTable? MarkFromSchema(DataTable? table, FormTable? formTable)
        {
            if (table != null && formTable != null)
                formTable.ApplyFieldDbTypes(table);
            return table;
        }

        private static DataTable BuildEmptyDataTable(FormTable formTable)
        {
            var dataTable = new DataTable(formTable.TableName);
            if (formTable.Fields == null)
                return dataTable;

            foreach (FormField field in formTable.Fields)
            {
                // The skeleton mirrors the GetData SELECT shape: persistent columns
                // plus relation display fields (`ref_*`), which the client lookup
                // write-back fills locally — without the column the write is silently
                // dropped and the picked value never shows on a new record. Virtual
                // (calculated) fields stay excluded; the command builders filter by
                // `FieldType.DbField`, so the extra columns never reach the SQL.
                if (field.Type == FieldType.VirtualField)
                    continue;
                dataTable.AddColumn(field.FieldName, field.DbType);
            }

            return dataTable;
        }

        private static void ApplyMasterDefaults(DataRow row, FormTable formTable)
        {
            if (formTable.Fields == null)
                return;

            foreach (FormField field in formTable.Fields)
            {
                if (field.Type != FieldType.DbField)
                    continue;
                if (!row.Table.Columns.Contains(field.FieldName))
                    continue;
                if (StringUtilities.IsEmpty(field.DefaultValue))
                    continue;

                var column = row.Table.Columns[field.FieldName]!;
                row[field.FieldName] = ConvertDefaultValue(field.DefaultValue, column.DataType);
            }
        }

        private static object ConvertDefaultValue(string raw, Type targetType)
        {
            if (targetType == typeof(string))
                return raw;
            if (targetType == typeof(Guid))
                return Guid.TryParse(raw, out var g) ? g : Guid.Empty;
            try
            {
                return Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return DBNull.Value;
            }
            catch (InvalidCastException)
            {
                return DBNull.Value;
            }
        }
    }
}
