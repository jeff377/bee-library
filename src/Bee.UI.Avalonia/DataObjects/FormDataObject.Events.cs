using System.Data;
using Bee.Api.Client;
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
            FormRowDefaults.Apply(formTable, e.Row, ResolveMasterRowId(),
                Bee.UI.Core.ClientInfo.UserInfo?.TimeZone ?? string.Empty);
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
                FormValueBinding.ToBindingString(e.ProposedValue),
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
    }
}
