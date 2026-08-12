using System.Data;
using Avalonia;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// The control's outward surface: binding to a data object, refresh, edit state and row commands, plus the tree hooks that keep them live.
    /// </summary>
    /// <remarks>
    /// This is what a view calls. The rendering side stays in `.Cells` / `.Columns`; the popup row editor
    /// in `.Rows`. Keeping the tree hooks here is deliberate — unsubscribing on detach is what stops a
    /// recycled template from driving a stale data object.
    /// </remarks>
    public partial class GridControl
    {
        /// <summary>
        /// Binds a detail table: resolves the <see cref="System.Data.DataTable"/> named
        /// <see cref="LayoutGrid.TableName"/> from the data object's <c>DataSet</c>.
        /// A missing table binds as empty (headers only) rather than throwing, because
        /// server responses may omit detail tables that carry no rows.
        /// </summary>
        /// <param name="dataObject">The data object whose dataset holds the detail table.</param>
        /// <param name="layout">The grid layout that defines the columns.</param>
        public void Bind(FormDataObject dataObject, LayoutGrid layout)
        {
            ArgumentNullException.ThrowIfNull(dataObject);
            ArgumentNullException.ThrowIfNull(layout);
            _layout = layout;
            TableName = layout.TableName;
            // Bind before building columns: lookup-column detection resolves the
            // FormField metadata through the bound data object.
            _binder.BindExplicit(dataObject);
            RebuildColumns();
            RefreshFromDataObject();
            // Initialise the editing state from the current ambient form mode. Without
            // this, AllowEdit stays at its default until a FormMode *change* is raised, so
            // an explicitly-bound grid whose host never drives FormMode (its ambient value
            // stays at the default) would keep the EditForm toolbar hidden. Mirrors the
            // list-mode Bind overload, which already self-initialises.
            SetControlState(GetValue(FormScope.FormModeProperty));
        }

        /// <summary>
        /// Binds a caller-supplied table (list mode): the rows of a
        /// <c>GetListAsync</c> response live outside any <see cref="FormDataObject"/>,
        /// so the grid never edits them and the toolbar stays hidden.
        /// </summary>
        /// <param name="layout">The grid layout that defines the columns.</param>
        /// <param name="rows">The data rows to render, or <c>null</c> for headers only.</param>
        public void Bind(LayoutGrid layout, DataTable? rows)
        {
            ArgumentNullException.ThrowIfNull(layout);
            // List-mode rows live outside any data object; drop a previous detail
            // subscription so a stale DataSetReplaced cannot overwrite these rows.
            _binder.Unbind();
            _layout = layout;
            TableName = layout.TableName;
            _dataTable = rows;
            RebuildColumns();
            RebuildRows();
            SetControlState(GetValue(FormScope.FormModeProperty));
        }

        /// <summary>
        /// Releases the data object subscription (if any). The current columns and
        /// rows stay rendered.
        /// </summary>
        public void Unbind()
        {
            _binder.Unbind();
        }

        /// <summary>
        /// Re-realizes the rows. Hosts call this after mutating row values outside
        /// the grid (for example after a popup edit form commits) — realized text
        /// cells capture their value when their template builds and do not track
        /// later <see cref="DataRow"/> writes.
        /// </summary>
        public void RefreshRows()
        {
            _grid.ItemsSource = null;
            RebuildRows();
        }

        /// <summary>
        /// Commits the in-progress cell and row edit (if any) so the underlying
        /// <see cref="DataRow"/> leaves its edit state before the host inspects or
        /// persists the dataset.
        /// </summary>
        public void EndEdit()
        {
            _endActiveInlineEdit?.Invoke();
            _grid.CommitEdit();
            if (_grid.SelectedItem is DataRowView { IsEdit: true } rowView)
                rowView.EndEdit();
        }

        /// <inheritdoc />
        public void SetControlState(SingleFormMode formMode)
        {
            // The layout's AllowEditModes narrows which form modes may edit; without
            // a layout the mode alone cannot grant editing.
            AllowEdit = formMode != SingleFormMode.View
                && (_layout?.AllowEditModes.Allows(formMode) ?? false);
            // The property handler skips unchanged values, but bind-time calls still
            // need the effective state re-evaluated against the (possibly new)
            // layout and data object.
            UpdateControlState();
        }

        /// <summary>
        /// Appends a new row to the bound table. The owning <see cref="FormDataObject"/> seeds the
        /// row's non-null columns from the FormSchema (a fresh <c>sys_rowid</c>, the
        /// <c>sys_master_rowid</c> master link, and type-appropriate defaults) through its
        /// <see cref="DataTable.TableNewRow"/> hook, so the row is insert-ready on creation.
        /// </summary>
        public void AddRow()
        {
            if (_dataTable is null) return;

            var row = _dataTable.NewRow();
            // Form-backed binds: the FormDataObject seeds the row's non-null columns (a fresh
            // sys_rowid, the sys_master_rowid link, and type-appropriate defaults) from the
            // FormSchema via its TableNewRow hook. Raw-table (list-mode) binds have no such hook,
            // so the grid fills non-nullable columns itself with type-appropriate empty values.
            if (_binder.DataObject is null)
            {
                foreach (DataColumn column in _dataTable.Columns)
                {
                    if (!column.AllowDBNull
                        && (column.DefaultValue is null || column.DefaultValue == DBNull.Value))
                    {
                        row[column] = FormDataObject.ResolveEmptyValueForType(column.DataType);
                    }
                }
            }
            // Attaching the row marks the data object dirty through its DataTable
            // event bridge; no explicit notification is needed here.
            _dataTable.Rows.Add(row);
            // The DataGrid does not observe DataView changes — re-realize so the
            // new row shows up.
            RefreshRows();
        }

        /// <summary>
        /// Deletes the selected row (marks it <see cref="DataRowState.Deleted"/> so the
        /// save pipeline can translate the change). No-op when nothing is selected.
        /// </summary>
        public void DeleteSelectedRow()
        {
            if (_grid.SelectedItem is not DataRowView rowView) return;
            rowView.Row.Delete();
            // The DataGrid does not observe DataView changes — re-realize so the
            // deleted row disappears.
            RefreshRows();
        }

        /// <inheritdoc />
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            _binder.NotifyAttached();
        }

        /// <inheritdoc />
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            _binder.NotifyDetached();
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            // `Geometry.Parse` needs platform services that are absent when unit
            // tests construct the control without an Avalonia platform, so the icon
            // data is created on first visual attach instead of in the constructor.
            _addIcon.Data ??= Geometry.Parse(AddIconGeometry);
            _editIcon.Data ??= Geometry.Parse(EditIconGeometry);
            _deleteIcon.Data ??= Geometry.Parse(DeleteIconGeometry);
        }
    }
}
