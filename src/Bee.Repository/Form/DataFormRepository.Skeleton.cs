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
        /// Guid columns are converted rather than only marked, because Oracle has no UUID type: the
        /// framework maps <see cref="FieldDbType.Guid"/> to <c>RAW(16)</c>, which reads back as
        /// <see cref="byte"/>[]. Left alone, a table from Oracle would declare a column Guid while
        /// holding byte arrays, and every consumer that reads the value — the client-side row guard,
        /// the grids in each UI head, a caller's own `is Guid` test — would take the else branch on
        /// that one provider.
        /// </para>
        /// </remarks>
        private static DataTable? MarkFromSchema(DataTable? table, FormTable? formTable)
        {
            if (table != null && formTable != null)
            {
                formTable.ApplyFieldDbTypes(table);
                NormalizeGuidColumns(table);
            }
            return table;
        }

        /// <summary>
        /// Rewrites columns the schema declares as <see cref="FieldDbType.Guid"/> but that the
        /// provider materialised as <see cref="byte"/>[] into real <see cref="Guid"/> columns.
        /// </summary>
        /// <remarks>
        /// The byte order is the one <see cref="Guid.ToByteArray()"/> produced on the way in — see
        /// <c>DbCommandSpec.NormalizeParameterValue</c> — so the matching constructor round-trips it.
        /// A DataColumn's type is immutable once it holds data, hence the replace-and-copy.
        /// <see cref="DataTable.AcceptChanges"/> at the end is safe because every caller of
        /// <see cref="MarkFromSchema"/> passes a table just filled from a SELECT: the rows are
        /// Unchanged on arrival and must stay that way, and writing the copied values marks them
        /// Modified.
        /// </remarks>
        private static void NormalizeGuidColumns(DataTable table)
        {
            var pending = table.Columns.Cast<DataColumn>()
                .Where(c => c.DataType == typeof(byte[])
                         && c.GetDeclaredFieldDbType() == FieldDbType.Guid)
                .ToList();
            if (pending.Count == 0) return;

            foreach (var column in pending)
            {
                var name = column.ColumnName;
                var ordinal = column.Ordinal;
                var values = table.Rows.Cast<DataRow>()
                    .Select(row => row[column] is byte[] { Length: 16 } bytes ? new Guid(bytes) : (object)DBNull.Value)
                    .ToList();

                table.Columns.Remove(column);
                var replacement = table.Columns.Add(name, typeof(Guid));
                replacement.ApplyFieldDbType(FieldDbType.Guid);
                replacement.SetOrdinal(ordinal);

                for (int i = 0; i < values.Count; i++)
                    table.Rows[i][replacement] = values[i];
            }

            table.AcceptChanges();
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
