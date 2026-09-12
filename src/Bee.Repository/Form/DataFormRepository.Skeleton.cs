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
        /// <para>
        /// Some columns are converted rather than only marked, because the provider has no type for
        /// what the schema declares; see <see cref="NormalizeStorageColumns"/>. Left alone, such a table
        /// would declare one type while holding another, and every consumer that reads the value — the
        /// client-side row guard, the grids in each UI head, the time zone conversion, a caller's own
        /// `is Guid` or `is DateTime` test — would take the else branch on that one provider.
        /// </para>
        /// </remarks>
        private static DataTable? MarkFromSchema(DataTable? table, FormTable? formTable)
        {
            if (table != null && formTable != null)
            {
                formTable.ApplyFieldDbTypes(table);
                NormalizeStorageColumns(table);
            }
            return table;
        }

        /// <summary>
        /// Rewrites columns whose provider type is only the storage form of their declared
        /// <see cref="FieldDbType"/> into columns of the declared CLR type.
        /// </summary>
        /// <remarks>
        /// Oracle has no UUID type: the framework maps <see cref="FieldDbType.Guid"/> to <c>RAW(16)</c>,
        /// which reads back as <see cref="byte"/>[]. SQLite has no date type: <see cref="FieldDbType.Date"/>
        /// and <see cref="FieldDbType.DateTime"/> are stored as text and read back as <see cref="string"/>.
        /// The column type is judged from the table itself even when it has no rows, so an empty result
        /// has the same shape as a full one.
        /// <para>
        /// A DataColumn's type is immutable once it holds data, hence the replace-and-copy.
        /// <see cref="DataTable.AcceptChanges"/> at the end is safe because every caller of
        /// <see cref="MarkFromSchema"/> passes a table just filled from a SELECT: the rows are
        /// Unchanged on arrival and must stay that way, and writing the copied values marks them
        /// Modified.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">A date column holds text that is not a date.</exception>
        private static void NormalizeStorageColumns(DataTable table)
        {
            var pending = table.Columns.Cast<DataColumn>()
                .Select(column => (Column: column, Declared: column.GetDeclaredFieldDbType()))
                .Where(p => p.Declared.HasValue && IsStorageForm(p.Column.DataType, p.Declared.Value))
                .ToList();
            if (pending.Count == 0) return;

            foreach (var (column, declared) in pending)
                ReplaceColumn(table, column, declared!.Value);

            table.AcceptChanges();
        }

        private static bool IsStorageForm(Type dataType, FieldDbType declared) => declared switch
        {
            FieldDbType.Guid => dataType == typeof(byte[]),
            FieldDbType.Date or FieldDbType.DateTime => dataType == typeof(string),
            _ => false,
        };

        private static void ReplaceColumn(DataTable table, DataColumn column, FieldDbType declared)
        {
            var name = column.ColumnName;
            var ordinal = column.Ordinal;
            var values = table.Rows.Cast<DataRow>()
                .Select(row => FromStorageForm(row[column], declared, table.TableName, name))
                .ToList();

            table.Columns.Remove(column);
            var type = DbTypeConverter.ToType(declared);
            var replacement = new DataColumn(name, type);
            // Match the DateTime columns the framework builds itself; see `DataTableExtensions.AddColumn`.
            if (type == typeof(DateTime)) { replacement.DateTimeMode = DataSetDateTime.Unspecified; }
            replacement.ApplyFieldDbType(declared);
            table.Columns.Add(replacement);
            replacement.SetOrdinal(ordinal);

            for (int i = 0; i < values.Count; i++)
                table.Rows[i][replacement] = values[i];
        }

        private static object FromStorageForm(object value, FieldDbType declared, string tableName, string columnName)
        {
            if (declared == FieldDbType.Guid)
            {
                // The byte order is the one `Guid.ToByteArray` produced on the way in (see
                // `DbCommandSpec.NormalizeParameterValue`), so the matching constructor round-trips it.
                return value is byte[] { Length: 16 } bytes ? new Guid(bytes) : DBNull.Value;
            }

            if (value is not string text || StringUtilities.IsEmpty(text)) { return DBNull.Value; }

            // WARNING: text that is not a date throws instead of reading as NULL. A value that silently
            // disappears is the failure this conversion exists to remove, and a NULL written back by a
            // later save would destroy the stored text for good.
            var instant = ValueUtilities.CDateTime(text)
                ?? throw new InvalidOperationException(
                    $"Column '{columnName}' of table '{tableName}' is declared {declared} but holds text that is not a date.");
            return DateTime.SpecifyKind(instant, DateTimeKind.Unspecified);
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
