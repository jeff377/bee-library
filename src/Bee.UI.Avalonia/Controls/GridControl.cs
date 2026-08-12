using System.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Tabular control that renders a <see cref="LayoutGrid"/> definition over a
    /// <see cref="System.Data.DataTable"/>. Composes a built-in icon toolbar
    /// (Add / Edit / Delete, shown only while <see cref="AllowEdit"/> grants
    /// editing) above an inner native <see cref="DataGrid"/> exposed as
    /// <see cref="InnerGrid"/>. Implements the definition-layer
    /// <see cref="IBindTableControl"/> / <see cref="IUIControl"/> contracts.
    /// Raises <see cref="RowSelected"/> with the row's <see cref="SysFields.RowId"/>
    /// Guid when the user selects a row.
    /// </summary>
    /// <remarks>
    /// <see cref="AllowEdit"/> is the single editing switch: the form host flips it
    /// through <see cref="SetControlState"/> (only single-record forms carry a form
    /// mode), other hosts set the property directly. Toolbar visibility, in-cell
    /// editing and the edit-form flow all derive from it inside the control;
    /// list-mode binds (rows without a <see cref="FormDataObject"/>) never edit.
    /// <para>
    /// Each column uses a <see cref="DataGridTemplateColumn"/> with a
    /// <see cref="FuncDataTemplate{T}"/> that fetches the cell value from
    /// <see cref="DataRowView"/>'s indexer at render time. The straightforward
    /// <c>new Binding("[FieldName]")</c> path that WPF uses does <em>not</em>
    /// resolve against <see cref="DataRowView"/>'s string indexer in Avalonia 12 —
    /// the binding engine looks up CLR properties / typed indexers and never
    /// reaches <c>DataRowView.this[string]</c> — so cells silently render empty
    /// even though <see cref="DataGrid.ItemsSource"/> iterates the rows.
    /// See docs/adr/adr-020-avalonia-datagrid-binding-strategy.md for the
    /// full reasoning and trade-offs.
    /// </para>
    /// <para>
    /// In-grid editing is available when the grid is bound to a
    /// <see cref="FormDataObject"/> detail table, <see cref="AllowEdit"/> is set,
    /// and the layout grants <see cref="GridControlAllowActions.Edit"/>; list-mode
    /// binds stay read-only. An empty table renders headers only — hosts that need
    /// an "empty" hint overlay their own placeholder.
    /// </para>
    /// </remarks>
    public partial class GridControl : ContentControl, IBindTableControl, IUIControl
    {
        /// <summary>
        /// Identifies the <see cref="TableName"/> styled property.
        /// </summary>
        public static readonly StyledProperty<string> TableNameProperty =
            AvaloniaProperty.Register<GridControl, string>(nameof(TableName), string.Empty);

        /// <summary>
        /// Identifies the <see cref="EditMode"/> styled property.
        /// </summary>
        public static readonly StyledProperty<GridEditMode> EditModeProperty =
            AvaloniaProperty.Register<GridControl, GridEditMode>(nameof(EditMode), GridEditMode.InCell);

        /// <summary>
        /// Identifies the <see cref="AllowEdit"/> styled property.
        /// </summary>
        public static readonly StyledProperty<bool> AllowEditProperty =
            AvaloniaProperty.Register<GridControl, bool>(nameof(AllowEdit), false);

        // Simple geometric glyphs (24 grid) drawn for the toolbar, embedded so the
        // control renders the same under any application theme.
        private const string AddIconGeometry =
            "M12 4a1 1 0 0 1 1 1v6h6a1 1 0 1 1 0 2h-6v6a1 1 0 1 1-2 0v-6H5a1 1 0 1 1 0-2h6V5a1 1 0 0 1 1-1Z";
        private const string EditIconGeometry =
            "M16.77 3.16a2.5 2.5 0 0 1 3.54 0l.53.53a2.5 2.5 0 0 1 0 3.54l-1.42 1.41-4.06-4.06 1.41-1.42Z" +
            "M14.3 5.64l4.06 4.06-9.53 9.53a2 2 0 0 1-.94.53l-3.6.9a.75.75 0 0 1-.91-.9l.9-3.61a2 2 0 0 1 .53-.94L14.3 5.64Z";
        private const string DeleteIconGeometry =
            "M10 2.5h4A1.5 1.5 0 0 1 15.5 4v.5H20a1 1 0 1 1 0 2h-.55l-.88 13.2A2.5 2.5 0 0 1 16.08 22H7.92" +
            "a2.5 2.5 0 0 1-2.49-2.3L4.55 6.5H4a1 1 0 0 1 0-2h4.5V4A1.5 1.5 0 0 1 10 2.5Z" +
            "M6.56 6.5l.86 12.97a.5.5 0 0 0 .5.46h8.16a.5.5 0 0 0 .5-.46l.86-12.97H6.56Z" +
            "M10 9.5a1 1 0 0 1 1 1v6a1 1 0 1 1-2 0v-6a1 1 0 0 1 1-1Zm4 0a1 1 0 0 1 1 1v6a1 1 0 1 1-2 0v-6a1 1 0 0 1 1-1Z";
        // Magnifier glyph for the trailing lookup-cell icon, matching the standalone
        // ButtonEdit's cue. Taken from Semi.Avalonia `SemiIconSearchStroked` (MIT).
        private const string LookupIconGeometry =
            "M16 10a6 6 0 1 1-12 0 6 6 0 0 1 12 0Zm-1.1 6.32a8 8 0 1 1 1.41-1.41l5.4 5.38a1 1 0 0 1-1.42 1.42l-5.38-5.39Z";

        private readonly GridControlBinder _binder;
        private readonly DataGrid _grid;
        private readonly StackPanel _toolbar;
        private readonly Button _addButton;
        private readonly Button _editButton;
        private readonly Button _deleteButton;
        private readonly PathIcon _addIcon;
        private readonly PathIcon _editIcon;
        private readonly PathIcon _deleteIcon;
        private LayoutGrid? _layout;
        private DataTable? _dataTable;
        private Action? _endActiveInlineEdit;

        static GridControl()
        {
            TableNameProperty.Changed.AddClassHandler<GridControl>((o, _) => o._binder.OnBindingContextChanged());
            EditModeProperty.Changed.AddClassHandler<GridControl>((o, _) => o.UpdateControlState());
            AllowEditProperty.Changed.AddClassHandler<GridControl>((o, _) => o.UpdateControlState());
            FormScope.DataObjectProperty.Changed.AddClassHandler<GridControl>((o, _) => o._binder.OnBindingContextChanged());
            FormScope.FormModeProperty.Changed.AddClassHandler<GridControl>((o, e) => o.SetControlState((SingleFormMode)e.NewValue!));
        }

        /// <summary>
        /// Gets or sets the client-side currency master used for per-cell decimal resolution of
        /// <see cref="NumberKind.Amount"/> columns. When <c>null</c> (the default), amount columns use
        /// their delivered <see cref="LayoutFieldBase.NumberFormat"/> unchanged — currency awareness is off.
        /// Hosts set this once the currency master is available and call <see cref="RefreshRows"/> after
        /// changing the document currency.
        /// </summary>
        public CurrencySettings? CurrencySettings { get; set; }

        /// <summary>
        /// Gets or sets the fallback currency code for amount cells whose row carries no currency-key
        /// field (for example a detail grid sharing the master document currency, or a grid with no
        /// per-row currency column). The per-row currency field (<see cref="LayoutFieldBase.CurrencyField"/>)
        /// wins when present; this is the "master document currency / company default" fallback.
        /// </summary>
        public string DefaultCurrencyCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the client-side unit-of-measure master used for per-cell decimal resolution of
        /// <see cref="NumberKind.Quantity"/> / <see cref="NumberKind.Weight"/> columns that bind a unit
        /// field. When <c>null</c> (the default), such columns use their delivered
        /// <see cref="LayoutFieldBase.NumberFormat"/> unchanged — unit awareness is off. Cells whose
        /// bound unit field is empty also keep the delivered (company-fallback) format.
        /// </summary>
        public UnitSettings? UnitSettings { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="GridControl"/> with a hidden
        /// toolbar and a read-only, single-selection inner grid.
        /// </summary>
        public GridControl()
        {
            _binder = new GridControlBinder(this);

            _grid = new DataGrid
            {
                IsReadOnly = true,
                AutoGenerateColumns = false,
                CanUserResizeColumns = true,
                SelectionMode = DataGridSelectionMode.Single,
            };
            _grid.SelectionChanged += OnSelectionChangedCore;
            _grid.DoubleTapped += async (_, _) =>
            {
                if (!CanUseEditForm) return;
                await EditSelectedRowAsync().ConfigureAwait(true);
            };

            _addIcon = BuildToolbarIcon();
            _editIcon = BuildToolbarIcon();
            _deleteIcon = BuildToolbarIcon();
            _addButton = BuildToolbarButton(_addIcon, "Add");
            _editButton = BuildToolbarButton(_editIcon, "Edit");
            _deleteButton = BuildToolbarButton(_deleteIcon, "Delete");
            _addButton.Click += async (_, _) => await AddRowAsync().ConfigureAwait(true);
            _editButton.Click += async (_, _) => await EditSelectedRowAsync().ConfigureAwait(true);
            _deleteButton.Click += (_, _) =>
            {
                EndEdit();
                DeleteSelectedRow();
            };

            _toolbar = new StackPanel
            {
                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                Spacing = 4,
                Margin = new Thickness(0, 0, 0, 4),
                IsVisible = false,
            };
            _toolbar.Children.Add(_addButton);
            _toolbar.Children.Add(_editButton);
            _toolbar.Children.Add(_deleteButton);

            var host = new DockPanel();
            DockPanel.SetDock(_toolbar, Dock.Top);
            host.Children.Add(_toolbar);
            host.Children.Add(_grid);
            Content = host;
        }

        /// <inheritdoc />
        // WARNING: Without this override the subclass looks up a ControlTheme keyed by
        // its own type, which the application theme does not provide, and the control
        // renders with no visual at all.
        protected override Type StyleKeyOverride => typeof(ContentControl);

        /// <summary>
        /// Gets the inner native <see cref="DataGrid"/>. Columns, items and
        /// selection internals live here; the composite keeps only the bound-grid
        /// surface (<see cref="Bind(FormDataObject, LayoutGrid)"/>,
        /// <see cref="AllowEdit"/>, row actions) on itself.
        /// </summary>
        public DataGrid InnerGrid => _grid;

        /// <summary>
        /// Gets or sets the bound table name.
        /// </summary>
        public string TableName
        {
            get => GetValue(TableNameProperty);
            set => SetValue(TableNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the editing model. <see cref="GridEditMode.EditForm"/> keeps
        /// the inner grid read-only; rows are edited in a popup edit form opened by
        /// the toolbar Edit button or a double tap.
        /// </summary>
        public GridEditMode EditMode
        {
            get => GetValue(EditModeProperty);
            set => SetValue(EditModeProperty, value);
        }

        /// <summary>
        /// Gets or sets whether editing is allowed. This is the single switch the
        /// host controls: single-record forms flip it through
        /// <see cref="SetControlState"/> on form-mode changes, other hosts set it
        /// directly. The toolbar, in-cell editing and the edit-form flow all follow
        /// it, further gated by the layout's <see cref="LayoutGrid.AllowActions"/>
        /// and the presence of a bound <see cref="FormDataObject"/>.
        /// </summary>
        public bool AllowEdit
        {
            get => GetValue(AllowEditProperty);
            set => SetValue(AllowEditProperty, value);
        }

        /// <summary>
        /// Gets or sets the bound data table. Setting the table rebuilds the rows;
        /// the columns keep following the layout supplied to <c>Bind</c>.
        /// </summary>
        public DataTable? DataTable
        {
            get => _dataTable;
            set
            {
                _dataTable = value;
                RebuildRows();
            }
        }

        /// <summary>
        /// Gets the layout definition supplied to <c>Bind</c>, or <c>null</c>.
        /// </summary>
        public LayoutGrid? Layout => _layout;

        /// <summary>
        /// Gets or sets the selected item of the inner grid.
        /// </summary>
        public object? SelectedItem
        {
            get => _grid.SelectedItem;
            set => _grid.SelectedItem = value;
        }

        /// <summary>
        /// Raised when the user selects a row; carries the row's
        /// <see cref="SysFields.RowId"/> Guid. Rows without a parseable
        /// <c>sys_rowid</c> are silently ignored.
        /// </summary>
        public event EventHandler<Guid>? RowSelected;

        private static PathIcon BuildToolbarIcon()
            => new()
            {
                Width = 14,
                Height = 14,
            };

        private static Button BuildToolbarButton(PathIcon icon, string toolTip)
        {
            var button = new Button
            {
                Content = icon,
                Focusable = false,
                Padding = new Thickness(6, 4),
                MinWidth = 0,
                MinHeight = 0,
            };
            ToolTip.SetTip(button, toolTip);
            return button;
        }
    }
}
