using System.Data;
using System.Globalization;
using Bee.Base;
using Bee.Base.Data;
using Bee.Base.Exceptions;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Repository.Abstractions.Form;

namespace Bee.Business.Form
{
    /// <summary>
    /// Replaces the <see cref="FieldDbType.DateTime"/> values of a data set about to be saved with
    /// the values the server owns: a fresh reading for new rows, the stored value for existing ones.
    /// </summary>
    /// <remarks>
    /// The caller's values are not trusted to be in any particular zone. A client holds its data set
    /// in the user's zone (ADR-032 D4), and nothing in the payload says which zone a value came from,
    /// so the server does not read them at all.
    /// </remarks>
    internal static class SaveDateTimeNormalizer
    {
        /// <summary>
        /// The text form a SQLite column holds an instant in.
        /// </summary>
        /// <remarks>
        /// NOTE: SQLite has no date type, so a table read back from it carries its instant columns as
        /// <see cref="string"/>. A value written into such a column must use the form the SQLite driver
        /// itself writes a <see cref="DateTime"/> parameter in, or text comparison and ordering in SQL
        /// break. A cast through the column's culture would produce a locale-dependent form.
        /// </remarks>
        private const string SqliteInstantFormat = "yyyy-MM-dd HH:mm:ss.FFFFFFF";

        /// <summary>
        /// Normalizes every <see cref="FieldDbType.DateTime"/> field the schema declares.
        /// </summary>
        /// <param name="schema">The form schema.</param>
        /// <param name="dataSet">The data set about to be saved; rewritten in place.</param>
        /// <param name="repository">The repository the stored values are read from.</param>
        /// <param name="utcNow">The single reading used for every stamped value in this save.</param>
        /// <exception cref="UserMessageException">A modified or deleted row no longer exists.</exception>
        public static void Normalize(FormSchema schema, DataSet dataSet, IDataFormRepository repository, DateTime utcNow)
        {
            if (schema.Tables == null) { return; }

            foreach (var formTable in schema.Tables)
            {
                if (!dataSet.Tables.Contains(formTable.TableName)) { continue; }

                var table = dataSet.Tables[formTable.TableName]!;
                var fields = DateTimeFields(formTable, table);
                if (fields.Count == 0) { continue; }

                NormalizeAddedRows(table, fields, utcNow);
                NormalizeStoredRows(formTable, table, fields, repository, utcNow);
            }
        }

        /// <summary>
        /// The persisted <see cref="FieldDbType.DateTime"/> fields of one table that the data set carries.
        /// </summary>
        /// <remarks>
        /// A column with an expression computes its own value and rejects a write, and the repository
        /// never writes a relation or virtual field, so neither is touched.
        /// </remarks>
        private static List<(FormField Field, DataColumn Column)> DateTimeFields(FormTable formTable, DataTable table)
        {
            var fields = new List<(FormField, DataColumn)>();
            if (formTable.Fields == null) { return fields; }

            foreach (var field in formTable.Fields)
            {
                if (field.Type != FieldType.DbField || field.DbType != FieldDbType.DateTime) { continue; }

                var column = table.Columns[field.FieldName];
                if (column == null || column.Expression.Length > 0) { continue; }
                fields.Add((field, column));
            }
            return fields;
        }

        /// <summary>
        /// Gives each new row its server values: the current reading for the system timestamps and for
        /// any field with no default-value expression, and an empty cell for the expression to fill.
        /// </summary>
        /// <remarks>
        /// A field with no expression receives the current reading rather than an empty cell, which
        /// matches what <see cref="FormRowDefaults"/> seeds on a new row and keeps a NOT NULL column
        /// from receiving NULL.
        /// </remarks>
        private static void NormalizeAddedRows(DataTable table, List<(FormField Field, DataColumn Column)> fields, DateTime utcNow)
        {
            foreach (DataRow row in table.Rows)
            {
                if (row.RowState != DataRowState.Added) { continue; }

                foreach (var (field, column) in fields)
                {
                    row[column] = !IsTimestamp(field) && StringUtilities.IsNotEmpty(field.DefaultValueExpression)
                        ? DBNull.Value
                        : CellValue(column, utcNow);
                }
            }
        }

        /// <summary>
        /// Sets both versions of every instant field on modified and deleted rows to the stored value,
        /// then stamps <see cref="SysFields.UpdateTime"/> on the modified ones.
        /// </summary>
        /// <exception cref="UserMessageException">A row is no longer in the database.</exception>
        private static void NormalizeStoredRows(FormTable formTable, DataTable table,
            List<(FormField Field, DataColumn Column)> fields, IDataFormRepository repository, DateTime utcNow)
        {
            var pending = table.Rows.Cast<DataRow>()
                .Where(row => row.RowState is DataRowState.Modified or DataRowState.Deleted)
                .ToList();
            if (pending.Count == 0) { return; }

            if (!table.Columns.Contains(SysFields.RowId))
            {
                throw new InvalidOperationException(
                    $"Table '{table.TableName}' has modified or deleted rows but no '{SysFields.RowId}' column; " +
                    "their stored values cannot be read back.");
            }

            var stored = ReadStoredRows(formTable, fields, pending, repository);
            foreach (var row in pending)
            {
                if (!stored.TryGetValue(StoredRowId(row), out var storedRow))
                {
                    throw new UserMessageException(
                        "The record was changed or deleted by someone else while it was open. Reload it and try again.");
                }

                if (row.RowState == DataRowState.Deleted)
                    RestoreDeletedRow(row, fields, storedRow);
                else
                    RestoreModifiedRow(row, fields, storedRow, utcNow);
            }
        }

        private static Dictionary<Guid, DataRow> ReadStoredRows(FormTable formTable,
            List<(FormField Field, DataColumn Column)> fields, List<DataRow> pending, IDataFormRepository repository)
        {
            var rowIds = pending.Select(StoredRowId).ToList();
            var selectFields = string.Join(",", fields.Select(f => f.Field.FieldName));
            var table = repository.GetRowsByRowId(formTable.TableName, selectFields, rowIds);

            var byRowId = new Dictionary<Guid, DataRow>();
            foreach (DataRow row in table.Rows)
            {
                byRowId[ValueUtilities.CGuid(row[SysFields.RowId])] = row;
            }
            return byRowId;
        }

        /// <summary>
        /// The row id the repository matches the row against, which is the Original version for both
        /// states the UPDATE and DELETE commands handle.
        /// </summary>
        private static Guid StoredRowId(DataRow row)
            => ValueUtilities.CGuid(row[SysFields.RowId, DataRowVersion.Original]);

        /// <summary>
        /// Rewrites both versions of a modified row, leaving it Modified with every other edit intact.
        /// </summary>
        /// <remarks>
        /// ADO.NET offers no way to write the Original version directly, so the row is rejected back to
        /// Original, given the new Original values, accepted, then given its Current values again.
        /// <para>
        /// WARNING: <see cref="DataRow.RejectChanges"/> reverts every column, not only the instant ones,
        /// so both versions are captured across the whole row before it runs. Restoring only the instant
        /// columns would silently discard every other edit on the row.
        /// </para>
        /// </remarks>
        private static void RestoreModifiedRow(DataRow row, List<(FormField Field, DataColumn Column)> fields,
            DataRow storedRow, DateTime utcNow)
        {
            var original = CaptureRow(row, DataRowVersion.Original);
            var current = CaptureRow(row, DataRowVersion.Current);
            foreach (var (field, column) in fields)
            {
                var storedValue = StoredCellValue(column, storedRow, field.FieldName);
                original[column.Ordinal] = storedValue;
                current[column.Ordinal] = IsField(field, SysFields.UpdateTime) ? CellValue(column, utcNow) : storedValue;
            }

            row.RejectChanges();
            WriteRow(row, original);
            row.AcceptChanges();
            WriteRow(row, current);

            // The repository picks UPDATE from the row state alone, so a row whose two versions now hold
            // equal values must stay Modified even though the write above changed nothing.
            if (row.RowState == DataRowState.Unchanged) { row.SetModified(); }
        }

        /// <summary>
        /// Rewrites the Original version of a deleted row, which is the only version it exposes and the
        /// one the change audit records.
        /// </summary>
        private static void RestoreDeletedRow(DataRow row, List<(FormField Field, DataColumn Column)> fields, DataRow storedRow)
        {
            row.RejectChanges();
            foreach (var (field, column) in fields)
            {
                var storedValue = StoredCellValue(column, storedRow, field.FieldName);
                if (!Equals(row[column], storedValue)) { row[column] = storedValue; }
            }
            row.AcceptChanges();
            row.Delete();
        }

        private static object[] CaptureRow(DataRow row, DataRowVersion version)
        {
            var values = new object[row.Table.Columns.Count];
            foreach (DataColumn column in row.Table.Columns)
            {
                values[column.Ordinal] = row[column, version];
            }
            return values;
        }

        /// <summary>
        /// Writes the columns whose value differs from what the row holds now.
        /// </summary>
        /// <remarks>
        /// Skipping equal values is what lets this run over the whole row: an expression column
        /// computes its own value and rejects a write, and a read-only column that did not change is
        /// never assigned, so it cannot raise <see cref="ReadOnlyException"/>.
        /// </remarks>
        private static void WriteRow(DataRow row, object[] values)
        {
            foreach (DataColumn column in row.Table.Columns)
            {
                if (column.Expression.Length > 0 || Equals(row[column], values[column.Ordinal])) { continue; }
                row[column] = values[column.Ordinal];
            }
        }

        private static object StoredCellValue(DataColumn column, DataRow storedRow, string fieldName)
        {
            var instant = ValueUtilities.CDateTime(storedRow[fieldName]);
            return instant.HasValue ? CellValue(column, instant.Value) : DBNull.Value;
        }

        /// <summary>
        /// Shapes an instant for the column it is written into.
        /// </summary>
        private static object CellValue(DataColumn column, DateTime instant)
        {
            var value = DateTime.SpecifyKind(instant, DateTimeKind.Unspecified);
            return column.DataType == typeof(string)
                ? value.ToString(SqliteInstantFormat, CultureInfo.InvariantCulture)
                : value;
        }

        private static bool IsTimestamp(FormField field)
            => IsField(field, SysFields.InsertTime) || IsField(field, SysFields.UpdateTime);

        private static bool IsField(FormField field, string name)
            => string.Equals(field.FieldName, name, StringComparison.OrdinalIgnoreCase);
    }
}
