using System.Data;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Reacting to data-object changes, and the add / edit flows that go through the popup row editor.
    /// </summary>
    /// <remarks>
    /// Apart from the binding surface because these are the asynchronous paths: they open a dialog, wait,
    /// and only then touch the grid — which is why each one re-reads state instead of capturing it.
    /// </remarks>
    public partial class GridControl
    {
        /// <summary>
        /// Re-resolves the bound table from the data object by <see cref="TableName"/>
        /// and refreshes the rows. Called after binding and whenever the underlying
        /// <c>DataSet</c> is replaced. Without a layout (ambient, table-name-only
        /// binds), plain text columns are generated from the table's columns.
        /// </summary>
        internal void RefreshFromDataObject()
        {
            var dataObject = _binder.DataObject;
            if (dataObject is null) return;

            var tables = dataObject.DataSet.Tables;
            var tableName = TableName;
            _dataTable = tableName.Length > 0 && tables.Contains(tableName) ? tables[tableName] : null;
            if (_layout is null)
                RebuildFallbackColumns();
            RebuildRows();
        }

        private bool CanUseEditForm
            => AllowEdit
                && EditMode == GridEditMode.EditForm
                && _binder.DataObject is not null
                && (_layout?.AllowActions.HasFlag(GridControlAllowActions.Edit) ?? false);

        // Recomputes the effective editing state after AllowEdit / EditMode / bind
        // changes: toolbar visibility per action flag, in-cell editability of the
        // inner grid.
        private void UpdateControlState()
        {
            var actions = _layout?.AllowActions ?? GridControlAllowActions.None;
            var canEdit = AllowEdit && _binder.DataObject is not null;

            _addButton.IsVisible = actions.HasFlag(GridControlAllowActions.Add);
            _deleteButton.IsVisible = actions.HasFlag(GridControlAllowActions.Delete);
            // In-cell editing needs no Edit button (cells edit in place); EditForm
            // surfaces one beside the double-tap gesture.
            _editButton.IsVisible = EditMode == GridEditMode.EditForm
                && actions.HasFlag(GridControlAllowActions.Edit);
            _toolbar.IsVisible = canEdit && actions != GridControlAllowActions.None;

            var inCellEdit = canEdit
                && EditMode == GridEditMode.InCell
                && actions.HasFlag(GridControlAllowActions.Edit);
            var readOnly = !inCellEdit;
            if (_grid.IsReadOnly == readOnly) return;

            _grid.IsReadOnly = readOnly;
            // Always-on editor cells capture the enabled state when their template
            // builds, so an editability switch must re-realize the rows.
            _grid.ItemsSource = null;
            RebuildRows();
        }

        private async Task AddRowAsync()
        {
            if (_binder.DataObject is null || _layout is null || _dataTable is null) return;
            AddRow();
            if (EditMode != GridEditMode.EditForm) return;

            var table = _dataTable;
            var row = table.Rows[table.Rows.Count - 1];
            var committed = await RowEditDialog.ShowAsync(this, _binder.DataObject, _layout, row).ConfigureAwait(true);
            if (committed)
            {
                RefreshAndFocusRow(row);
            }
            else
            {
                // A cancelled Add removes the blank row again instead of leaving an
                // empty line in the detail table.
                table.Rows.Remove(row);
                RefreshRows();
            }
        }

        private async Task EditSelectedRowAsync()
        {
            if (_binder.DataObject is null || _layout is null) return;
            if (_grid.SelectedItem is not DataRowView rowView) return;

            var row = rowView.Row;
            var committed = await RowEditDialog.ShowAsync(this, _binder.DataObject, _layout, row).ConfigureAwait(true);
            if (committed)
                RefreshAndFocusRow(row);
        }

        // Realized text cells capture their value at template build, so a committed
        // edit form re-realizes the rows and scrolls back to the affected row.
        private void RefreshAndFocusRow(DataRow row)
        {
            RefreshRows();
            var rowView = _dataTable?.DefaultView
                .Cast<DataRowView>()
                .FirstOrDefault(v => ReferenceEquals(v.Row, row));
            if (rowView is null) return;
            _grid.SelectedItem = rowView;
            _grid.ScrollIntoView(rowView, null);
        }
    }
}
