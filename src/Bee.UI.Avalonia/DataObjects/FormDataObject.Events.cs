using System.Data;
using System.Globalization;
using Bee.Api.Client.Connectors;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Forms;

namespace Bee.UI.Avalonia.DataObjects
{
    /// <summary>
    /// DataTable-event and value-conversion half of <see cref="FormDataObject"/>. Split out for file
    /// size only; behaviour is unchanged.
    /// </summary>
    public partial class FormDataObject
    {
        /// <summary>
        /// The single assignment point for <see cref="DataSet"/>: moves the table
        /// event subscriptions from the old dataset to the new one (subscribing only
        /// after the server has fully populated it, so loading raises nothing) and
        /// notifies subscribers.
        /// </summary>
        private void ReplaceDataSet(DataSet dataSet, bool notify = true)
        {
            DetachTableEvents(DataSet);
            _rowsInEdit.Clear();
            DataSet = dataSet;
            AttachTableEvents(dataSet);
            if (notify)
                DataSetReplaced?.Invoke(this, EventArgs.Empty);
        }

        private void AttachTableEvents(DataSet dataSet)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                table.TableNewRow += OnTableNewRow;
                table.ColumnChanged += OnTableColumnChanged;
                table.RowChanged += OnTableRowChanged;
                table.RowDeleted += OnTableRowDeleted;
            }
        }

        private void DetachTableEvents(DataSet? dataSet)
        {
            if (dataSet is null) return;
            foreach (DataTable table in dataSet.Tables)
            {
                table.TableNewRow -= OnTableNewRow;
                table.ColumnChanged -= OnTableColumnChanged;
                table.RowChanged -= OnTableRowChanged;
                table.RowDeleted -= OnTableRowDeleted;
            }
        }

        /// <summary>
        /// Seeds a freshly created row's persisted columns with type-appropriate non-null values
        /// drawn from the FormSchema, so a new row never reaches the database with a NULL that would
        /// violate a NOT NULL constraint. <c>sys_rowid</c> gets a fresh key, <c>sys_master_rowid</c>
        /// links to the loaded master, and other columns default by <see cref="FieldDbType"/>
        /// (text → empty string, numeric → 0, Date → today, DateTime → now, …). Columns that already
        /// carry a value (e.g. a schema-pinned default) are left untouched.
        /// </summary>
        private void OnTableNewRow(object? sender, DataTableNewRowEventArgs e)
        {
            var formTable = _schema.Tables?.GetOrDefault(e.Row.Table.TableName);
            if (formTable is null) return;

            // Same schema-driven seeding the server uses for the GetNewData master row, plus the
            // master link for a detail row (the client knows the loaded master).
            FormRowDefaults.Apply(formTable, e.Row, ResolveMasterRowId());
        }

        // The master row's sys_rowid a new detail row links through sys_master_rowid. Returns the
        // raw value (Guid or the provider's exact string form) so the link preserves the master's
        // casing — a re-parsed Guid would lowercase a string key and orphan the detail under a
        // case-sensitive comparison (e.g. SQLite stores GUIDs as case-sensitive TEXT).
        private object? ResolveMasterRowId()
        {
            var master = MasterRow;
            if (master is null || !master.Table.Columns.Contains(SysFields.RowId)) return null;
            var value = master[SysFields.RowId];
            return value == DBNull.Value ? null : value;
        }

        private void OnTableColumnChanged(object? sender, DataColumnChangeEventArgs e)
        {
            // Seeding a detached row (NewRow before Rows.Add) stays silent; attaching
            // the row marks dirty through RowChanged instead.
            if (e.Row.RowState == DataRowState.Detached) return;
            // Rows under an explicit edit session publish nothing until commit —
            // CommitRowEdit re-publishes the session's changes; a cancelled session
            // must leak no events for values that were rolled back.
            if (_rowsInEdit.Contains(e.Row)) return;

            IsDirty = true;
            FieldValueChanged?.Invoke(this, new FieldValueChangedEventArgs(
                ((DataTable)sender!).TableName,
                e.Column!.ColumnName,
                FormatForBinding(e.ProposedValue),
                e.Row));
        }

        private void OnTableRowChanged(object? sender, DataRowChangeEventArgs e)
        {
            // Only data mutations dirty the object; framework actions (AcceptChanges
            // raises Commit, RejectChanges raises Rollback) do not. Rows in an
            // explicit edit session dirty the object at commit, not per keystroke.
            if (_rowsInEdit.Contains(e.Row)) return;
            if (e.Action is DataRowAction.Add or DataRowAction.Change)
                IsDirty = true;
            if (e.Action == DataRowAction.Add)
                RowAdded?.Invoke(this, new RowChangedEventArgs(((DataTable)sender!).TableName, e.Row));
        }

        private void OnTableRowDeleted(object? sender, DataRowChangeEventArgs e)
        {
            IsDirty = true;
            RowDeleted?.Invoke(this, new RowChangedEventArgs(((DataTable)sender!).TableName, e.Row));
        }

        private FormApiConnector RequireConnector(string operation)
        {
            return _connector
                ?? throw new InvalidOperationException(
                    $"{operation} requires a FormApiConnector; pass one to the FormDataObject constructor.");
        }

        private Guid RequireMasterRowId()
        {
            var row = MasterRow
                ?? throw new InvalidOperationException("No master row is loaded; cannot delete.");
            if (!row.Table.Columns.Contains(SysFields.RowId))
                throw new InvalidOperationException(
                    $"Master table is missing the '{SysFields.RowId}' column; cannot delete.");

            var raw = row[SysFields.RowId];
            if (raw is null || raw == DBNull.Value)
                throw new InvalidOperationException(
                    $"Master row has a null '{SysFields.RowId}'; cannot delete.");

            return raw is Guid g ? g : Guid.Parse(raw.ToString()!);
        }

        private static DataSet BuildEmptyDataSet(FormSchema schema)
        {
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

        private static string FormatForBinding(object? raw)
        {
            if (raw is null || raw == DBNull.Value)
                return string.Empty;

            return raw switch
            {
                // ISO 8601 keeps round-trip parity with desktop DatePicker / TextBox controls.
                DateTime dt => dt.TimeOfDay == TimeSpan.Zero
                    ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => raw.ToString() ?? string.Empty,
            };
        }

        // NOTE: Internal (not private) so GridControl's in-cell editors reuse the same
        // string-to-column coercion rules instead of growing a divergent copy.
        internal static object ConvertToColumnValue(string? value, DataColumn column)
        {
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
                return ResolveEmptyValueForType(column.DataType);
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

        internal static object ResolveEmptyValueForType(Type targetType)
        {
            if (targetType == typeof(string)) return string.Empty;
            if (targetType == typeof(Guid)) return Guid.Empty;
            if (targetType == typeof(DateTime)) return DateTime.MinValue;
            if (targetType == typeof(byte[])) return Array.Empty<byte>();
            if (targetType.IsValueType) return Activator.CreateInstance(targetType)!;
            return DBNull.Value;
        }
    }
}
