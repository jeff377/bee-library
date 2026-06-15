using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Bee.Api.Client.Connectors;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Core;

namespace Bee.UI.Avalonia.Views
{
    /// <summary>
    /// Read-only browser for every record of a form (the master list). Pairs with
    /// <see cref="FormView"/> following the ERP convention of keeping list browsing and
    /// record viewing/editing on separate surfaces: <see cref="ListView"/> loads the list
    /// and raises <see cref="ViewRequested"/> / <see cref="EditRequested"/> /
    /// <see cref="AddRequested"/>, leaving the host to swap in the record surface. Delete is
    /// handled in place (the list stays the active surface). It carries no field form.
    /// </summary>
    /// <remarks>
    /// Like <see cref="FormView"/>, the host typically supplies only <see cref="ProgId"/>;
    /// when <see cref="Schema"/> / <see cref="FormConnector"/> / <see cref="AccessToken"/>
    /// are left unset, <see cref="InitializeAsync"/> resolves them from
    /// <see cref="ClientInfo"/> through the <c>Resolve*</c> hooks, which a subclass can
    /// override to bypass the static state (the unit tests rely on this).
    /// </remarks>
    public class ListView : UserControl
    {
        /// <summary>Identifies the <see cref="ProgId"/> styled property.</summary>
        public static readonly StyledProperty<string> ProgIdProperty =
            AvaloniaProperty.Register<ListView, string>(nameof(ProgId), defaultValue: string.Empty);

        /// <summary>Identifies the <see cref="AccessToken"/> styled property.</summary>
        public static readonly StyledProperty<Guid> AccessTokenProperty =
            AvaloniaProperty.Register<ListView, Guid>(nameof(AccessToken), defaultValue: Guid.Empty);

        /// <summary>Identifies the <see cref="Schema"/> styled property.</summary>
        public static readonly StyledProperty<FormSchema?> SchemaProperty =
            AvaloniaProperty.Register<ListView, FormSchema?>(nameof(Schema));

        /// <summary>Identifies the <see cref="FormConnector"/> styled property.</summary>
        public static readonly StyledProperty<FormApiConnector?> FormConnectorProperty =
            AvaloniaProperty.Register<ListView, FormApiConnector?>(nameof(FormConnector));

        private readonly Button _viewButton;
        private readonly Button _newButton;
        private readonly Button _editButton;
        private readonly Button _deleteButton;
        private readonly TextBlock _errorLabel;
        private readonly TextBlock _loadingLabel;
        private readonly TextBlock _emptyListLabel;
        private readonly GridControl _grid;
        private Guid _selectedRowId;
        private bool _isBusy;
        private bool _initialized;
        private bool _isInitializing;

        static ListView()
        {
            SchemaProperty.Changed.AddClassHandler<ListView>((v, _) => v.OnInputsChanged());
            FormConnectorProperty.Changed.AddClassHandler<ListView>((v, _) => v.OnInputsChanged());
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ListView"/> with the toolbar + grid
        /// assembled. The toolbar buttons stay disabled until <see cref="InitializeAsync"/>
        /// resolves the schema and connector inputs.
        /// </summary>
        public ListView()
        {
            _errorLabel = new TextBlock { Foreground = Brushes.Red, IsVisible = false };
            _loadingLabel = new TextBlock { Text = "Loading…", IsVisible = true };

            _viewButton = new Button { Content = "View", IsEnabled = false };
            _viewButton.Click += (_, _) => OnViewClicked();

            _newButton = new Button { Content = "New", IsEnabled = false };
            _newButton.Click += (_, _) => OnNewClicked();

            _editButton = new Button { Content = "Edit", IsEnabled = false };
            _editButton.Click += (_, _) => OnEditClicked();

            _deleteButton = new Button { Content = "Delete", IsEnabled = false };
            _deleteButton.Click += async (_, _) => await OnDeleteClickedAsync().ConfigureAwait(true);

            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
            };
            toolbar.Children.Add(_viewButton);
            toolbar.Children.Add(_newButton);
            toolbar.Children.Add(_editButton);
            toolbar.Children.Add(_deleteButton);

            _grid = new GridControl();
            _grid.RowSelected += (_, rowId) => OnRowSelected(rowId);
            // List mode never binds a FormDataObject, so the grid's own double-tap path
            // (detail EditForm editing) stays dormant; the bubbling DoubleTapped is ours to
            // treat as "open this record" — opening read-only (View), so browsing the list
            // never accidentally enters edit mode. Editing is the explicit Edit action.
            _grid.DoubleTapped += (_, _) => OnGridDoubleTapped();

            _emptyListLabel = new TextBlock { Text = "No data.", IsVisible = false };

            // DockPanel (not StackPanel): the chrome docks to the top and the grid fills the
            // remaining BOUNDED height as the last child. A StackPanel would give the grid
            // unbounded height, so the DataGrid grows to fit every row and never shows its
            // own vertical scrollbar.
            toolbar.Margin = new Thickness(0, 0, 0, 8);
            var host = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(_errorLabel, Dock.Top);
            DockPanel.SetDock(_loadingLabel, Dock.Top);
            DockPanel.SetDock(toolbar, Dock.Top);
            DockPanel.SetDock(_emptyListLabel, Dock.Top);
            host.Children.Add(_errorLabel);
            host.Children.Add(_loadingLabel);
            host.Children.Add(toolbar);
            host.Children.Add(_emptyListLabel);
            host.Children.Add(_grid);

            Content = host;
        }

        /// <summary>
        /// Gets or sets the program identifier (e.g. "Category"). Drives both the
        /// <see cref="ClientInfo"/>-backed FormSchema lookup and the
        /// <see cref="FormApiConnector"/> creation when the host leaves
        /// <see cref="Schema"/> / <see cref="FormConnector"/> unset.
        /// </summary>
        public string ProgId
        {
            get => GetValue(ProgIdProperty);
            set => SetValue(ProgIdProperty, value);
        }

        /// <summary>
        /// Gets or sets the access token surfaced on the view. Defaults to
        /// <see cref="Guid.Empty"/>; <see cref="InitializeAsync"/> fills it from
        /// <see cref="ClientInfo.AccessToken"/> when the host did not supply one.
        /// </summary>
        public Guid AccessToken
        {
            get => GetValue(AccessTokenProperty);
            set => SetValue(AccessTokenProperty, value);
        }

        /// <summary>Gets or sets the resolved <see cref="FormSchema"/>.</summary>
        public FormSchema? Schema
        {
            get => GetValue(SchemaProperty);
            set => SetValue(SchemaProperty, value);
        }

        /// <summary>Gets or sets the connector used for the list / delete round-trips.</summary>
        public FormApiConnector? FormConnector
        {
            get => GetValue(FormConnectorProperty);
            set => SetValue(FormConnectorProperty, value);
        }

        /// <summary>
        /// Raised when the user opens a record read-only (View on a selected row). Carries
        /// the record's <see cref="SysFields.RowId"/>.
        /// </summary>
        public event EventHandler<Guid>? ViewRequested;

        /// <summary>
        /// Raised when the user opens a record for editing (Edit on a selected row, or a
        /// double-click). Carries the record's <see cref="SysFields.RowId"/>.
        /// </summary>
        public event EventHandler<Guid>? EditRequested;

        /// <summary>Raised when the user presses New. The host opens a blank record surface.</summary>
        public event EventHandler? AddRequested;

        /// <summary>Raised whenever a backend round-trip throws.</summary>
        public event EventHandler<Exception>? ErrorOccurred;

        /// <summary>Resolves the schema + connector inputs and runs the initial list load.</summary>
        public async Task InitializeAsync()
        {
            if (_initialized || _isInitializing) return;

            var hasProgId = !string.IsNullOrEmpty(ProgId);
            if (!hasProgId && (Schema is null || FormConnector is null)) return;

            _isInitializing = true;
            try
            {
                ApplyAccessTokenFallback();

                if (!await TryResolveSchemaAsync(hasProgId).ConfigureAwait(true)) return;
                ResolveFormConnectorFallback(hasProgId);

                if (Schema is null || FormConnector is null) return;
                AttachGrid();

                _initialized = true;
                await ReloadAsync().ConfigureAwait(true);
                UpdateToolbarState();
            }
            finally
            {
                _isInitializing = false;
            }
        }

        /// <summary>Reloads the list from the backend, clearing any prior selection.</summary>
        public async Task ReloadAsync()
        {
            var connector = FormConnector;
            if (connector is null) return;

            try
            {
                var response = await connector.GetListAsync(ComputeSelectFields()).ConfigureAwait(true);
                _grid.DataTable = response.Table;
                var isEmpty = response.Table is null || response.Table.Rows.Count == 0;
                _grid.IsVisible = !isEmpty;
                _emptyListLabel.IsVisible = isEmpty;
                _selectedRowId = Guid.Empty;
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
            finally
            {
                UpdateToolbarState();
            }
        }

        /// <summary>
        /// Resolves the <see cref="SystemApiConnector"/> used to load the
        /// <see cref="FormSchema"/>. Override to bypass <see cref="ClientInfo"/>.
        /// </summary>
        protected virtual SystemApiConnector? ResolveSystemConnector() => ClientInfo.SystemApiConnector;

        /// <summary>
        /// Resolves the <see cref="FormApiConnector"/> for the list / delete round-trips.
        /// Override to bypass <see cref="ClientInfo"/>.
        /// </summary>
        protected virtual FormApiConnector ResolveFormConnector(string progId)
            => ClientInfo.CreateFormApiConnector(progId);

        /// <summary>Resolves the access token. Override to plug in a different session source.</summary>
        protected virtual Guid ResolveAccessToken() => ClientInfo.AccessToken;

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (!_initialized && !_isInitializing)
                _ = InitializeAsync();
        }

        private void OnInputsChanged()
        {
            if (_initialized || _isInitializing) return;
            _ = InitializeAsync();
        }

        private void ApplyAccessTokenFallback()
        {
            if (AccessToken != Guid.Empty) return;
            var fallbackToken = ResolveAccessToken();
            if (fallbackToken != Guid.Empty)
                AccessToken = fallbackToken;
        }

        private async Task<bool> TryResolveSchemaAsync(bool hasProgId)
        {
            if (Schema is not null || !hasProgId) return true;

            var systemConnector = ResolveSystemConnector();
            if (systemConnector is null) return true;

            ClearError();
            try
            {
                var key = new[] { ProgId };
                var loaded = await systemConnector
                    .GetDefineAsync<FormSchema>(DefineType.FormSchema, key)
                    .ConfigureAwait(true);
                if (loaded is not null)
                    Schema = loaded;
                return true;
            }
            catch (Exception ex)
            {
                ReportError(ex);
                return false;
            }
        }

        private void ResolveFormConnectorFallback(bool hasProgId)
        {
            if (FormConnector is not null || !hasProgId) return;
            FormConnector = ResolveFormConnector(ProgId);
        }

        private void AttachGrid()
        {
            ClearError();
            _loadingLabel.IsVisible = false;

            var listLayout = Schema!.GetListLayout();
            // Columns render immediately; rows arrive with the first ReloadAsync.
            _grid.Bind(listLayout, rows: null);
        }

        // FormSchema.ListFields drives the server SELECT but omits sys_rowid; prepend it so
        // the wire response carries the identifier the record events need (mirrors FormView).
        private string ComputeSelectFields()
        {
            var schema = Schema;
            if (schema is null) return string.Empty;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { SysFields.RowId };
            var parts = new List<string> { SysFields.RowId };
            foreach (var name in (schema.ListFields ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = name.Trim();
                if (trimmed.Length > 0 && seen.Add(trimmed))
                    parts.Add(trimmed);
            }
            return string.Join(",", parts);
        }

        private void OnRowSelected(Guid rowId)
        {
            _selectedRowId = rowId;
            UpdateToolbarState();
        }

        private void OnGridDoubleTapped()
        {
            // Opening a row from the list is a read-only View; editing is the explicit Edit
            // action (toolbar button), so clicking through the list never mutates by accident.
            if (_selectedRowId != Guid.Empty)
                ViewRequested?.Invoke(this, _selectedRowId);
        }

        private void OnViewClicked()
        {
            if (_selectedRowId != Guid.Empty)
                ViewRequested?.Invoke(this, _selectedRowId);
        }

        private void OnEditClicked()
        {
            if (_selectedRowId != Guid.Empty)
                EditRequested?.Invoke(this, _selectedRowId);
        }

        private void OnNewClicked()
        {
            if (!_initialized || _isBusy) return;
            AddRequested?.Invoke(this, EventArgs.Empty);
        }

        private async Task OnDeleteClickedAsync()
        {
            var connector = FormConnector;
            if (connector is null || _selectedRowId == Guid.Empty || _isBusy) return;

            _isBusy = true;
            ClearError();
            try
            {
                await connector.DeleteAsync(_selectedRowId).ConfigureAwait(true);
                await ReloadAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
            finally
            {
                _isBusy = false;
                UpdateToolbarState();
            }
        }

        private void UpdateToolbarState()
        {
            var hasSelection = _selectedRowId != Guid.Empty;
            _newButton.IsEnabled = _initialized && !_isBusy;
            _viewButton.IsEnabled = _initialized && !_isBusy && hasSelection;
            _editButton.IsEnabled = _initialized && !_isBusy && hasSelection;
            _deleteButton.IsEnabled = _initialized && !_isBusy && hasSelection;
        }

        private void ReportError(Exception ex)
        {
            _errorLabel.Text = ex.Message;
            _errorLabel.IsVisible = true;
            ErrorOccurred?.Invoke(this, ex);
        }

        private void ClearError()
        {
            _errorLabel.Text = string.Empty;
            _errorLabel.IsVisible = false;
        }
    }
}
