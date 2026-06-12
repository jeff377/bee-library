using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Bee.Api.Client.Connectors;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;
using Bee.UI.Core;

namespace Bee.UI.Avalonia.Controls
{
    /// <summary>
    /// Single-record data form that wires <see cref="GridControl"/> (list view)
    /// to <see cref="DynamicForm"/> (master detail) via a shared
    /// <see cref="FormDataObject"/>. Selecting a list row drives
    /// <see cref="FormDataObject.LoadAsync"/>; the toolbar buttons fan out to
    /// <see cref="FormDataObject.NewAsync"/> / <c>SaveAsync</c> / <c>DeleteAsync</c>.
    /// Mirrors the MAUI <c>FormPage</c> structure for cross-family parity.
    /// As a <see cref="SingleFormBase"/> it owns the form mode: loading a row enters
    /// View (read-only browsing), the Edit button enters Edit, New enters Add, and a
    /// successful Save returns to View — each transition broadcasts to every editor
    /// and grid in the form through the ambient <see cref="FormScope"/>.
    /// </summary>
    /// <remarks>
    /// The host typically supplies only <see cref="ProgId"/>. When <see cref="Schema"/>
    /// and <see cref="FormConnector"/> are left unset, <see cref="InitializeAsync"/>
    /// resolves them from <see cref="ClientInfo"/>:
    /// <c>SystemApiConnector.GetDefineAsync&lt;FormSchema&gt;</c> for the schema and
    /// <c>CreateFormApiConnector(ProgId)</c> for the connector. The same path applies
    /// to <see cref="AccessToken"/>, which falls back to <see cref="ClientInfo.AccessToken"/>
    /// when the host leaves it as <see cref="Guid.Empty"/>. Hosts that want to bypass
    /// the static <see cref="ClientInfo"/> can either supply <see cref="Schema"/> /
    /// <see cref="FormConnector"/> directly, or subclass and override the
    /// <c>ResolveSystemConnector</c> / <c>ResolveFormConnector</c> / <c>ResolveAccessToken</c>
    /// hooks below.
    /// </remarks>
    public class FormView : SingleFormBase
    {
        /// <summary>
        /// Identifies the <see cref="ProgId"/> styled property.
        /// </summary>
        public static readonly StyledProperty<string> ProgIdProperty =
            AvaloniaProperty.Register<FormView, string>(nameof(ProgId), defaultValue: string.Empty);

        /// <summary>
        /// Identifies the <see cref="AccessToken"/> styled property.
        /// </summary>
        public static readonly StyledProperty<Guid> AccessTokenProperty =
            AvaloniaProperty.Register<FormView, Guid>(nameof(AccessToken), defaultValue: Guid.Empty);

        /// <summary>
        /// Identifies the <see cref="Schema"/> styled property.
        /// </summary>
        public static readonly StyledProperty<FormSchema?> SchemaProperty =
            AvaloniaProperty.Register<FormView, FormSchema?>(nameof(Schema));

        /// <summary>
        /// Identifies the <see cref="FormConnector"/> styled property.
        /// </summary>
        public static readonly StyledProperty<FormApiConnector?> FormConnectorProperty =
            AvaloniaProperty.Register<FormView, FormApiConnector?>(nameof(FormConnector));

        private readonly Button _newButton;
        private readonly Button _editButton;
        private readonly Button _saveButton;
        private readonly Button _deleteButton;
        private readonly TextBlock _dirtyMarker;
        private readonly TextBlock _errorLabel;
        private readonly TextBlock _loadingLabel;
        private readonly TextBlock _emptyListLabel;
        private readonly GridControl _grid;
        private readonly DynamicForm _form;
        private FormDataObject? _dataObject;
        private LayoutGrid? _listLayout;
        private bool _isBusy;
        private bool _initialized;
        private bool _isInitializing;

        static FormView()
        {
            SchemaProperty.Changed.AddClassHandler<FormView>((v, _) => v.OnInputsChanged());
            FormConnectorProperty.Changed.AddClassHandler<FormView>((v, _) => v.OnInputsChanged());
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FormView"/> with the toolbar +
        /// list + form layout assembled. The toolbar buttons are disabled until
        /// <see cref="InitializeAsync"/> resolves the schema and connector inputs.
        /// </summary>
        public FormView()
        {
            _errorLabel = new TextBlock { Foreground = Brushes.Red, IsVisible = false };
            _loadingLabel = new TextBlock { Text = "Loading…", IsVisible = true };

            _newButton = new Button { Content = "New", IsEnabled = false };
            _newButton.Click += async (_, _) => await OnNewClickedAsync().ConfigureAwait(true);

            _editButton = new Button { Content = "Edit", IsEnabled = false };
            _editButton.Click += (_, _) => OnEditClicked();

            _saveButton = new Button { Content = "Save", IsEnabled = false };
            _saveButton.Click += async (_, _) => await OnSaveClickedAsync().ConfigureAwait(true);

            _deleteButton = new Button { Content = "Delete", IsEnabled = false };
            _deleteButton.Click += async (_, _) => await OnDeleteClickedAsync().ConfigureAwait(true);

            _dirtyMarker = new TextBlock
            {
                Text = "● unsaved",
                Foreground = Brushes.DarkGoldenrod,
                VerticalAlignment = VerticalAlignment.Center,
                IsVisible = false,
            };

            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
            };
            toolbar.Children.Add(_newButton);
            toolbar.Children.Add(_editButton);
            toolbar.Children.Add(_saveButton);
            toolbar.Children.Add(_deleteButton);
            toolbar.Children.Add(_dirtyMarker);

            _grid = new GridControl();
            _grid.RowSelected += async (_, rowId) => await OnRowSelectedAsync(rowId).ConfigureAwait(true);

            // GridControl renders headers only when the list is empty; the view keeps
            // the textual hint the retired DynamicGrid used to provide.
            _emptyListLabel = new TextBlock { Text = "No data.", IsVisible = false };

            _form = new DynamicForm();

            var host = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 12,
            };
            host.Children.Add(_errorLabel);
            host.Children.Add(_loadingLabel);
            host.Children.Add(toolbar);
            host.Children.Add(_grid);
            host.Children.Add(_emptyListLabel);
            host.Children.Add(_form);

            Content = host;
        }

        /// <summary>
        /// Gets or sets the program identifier (e.g. "Employee"). Drives both the
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
        /// <see cref="Guid.Empty"/> (anonymous); <see cref="InitializeAsync"/>
        /// fills this in from <see cref="ClientInfo.AccessToken"/> when the host
        /// did not supply one.
        /// </summary>
        public Guid AccessToken
        {
            get => GetValue(AccessTokenProperty);
            set => SetValue(AccessTokenProperty, value);
        }

        /// <summary>
        /// Gets or sets the resolved <see cref="FormSchema"/>.
        /// </summary>
        public FormSchema? Schema
        {
            get => GetValue(SchemaProperty);
            set => SetValue(SchemaProperty, value);
        }

        /// <summary>
        /// Gets or sets the connector used for all CRUD round-trips.
        /// </summary>
        public FormApiConnector? FormConnector
        {
            get => GetValue(FormConnectorProperty);
            set => SetValue(FormConnectorProperty, value);
        }

        /// <summary>
        /// Raised whenever a backend round-trip throws.
        /// </summary>
        public event EventHandler<Exception>? ErrorOccurred;

        /// <summary>
        /// Gets the data object built once <see cref="Schema"/> and
        /// <see cref="FormConnector"/> are both supplied.
        /// </summary>
        public FormDataObject? DataObject => _dataObject;

        /// <summary>
        /// Builds <see cref="FormDataObject"/> from the current <see cref="Schema"/>
        /// + <see cref="FormConnector"/> and runs the initial list reload.
        /// </summary>
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
                AttachDataObject();

                _initialized = true;
                await ReloadListAsync().ConfigureAwait(true);
                UpdateToolbarState();
            }
            finally
            {
                _isInitializing = false;
            }
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

        private void AttachDataObject()
        {
            ClearError();
            _loadingLabel.IsVisible = false;

            _dataObject = new FormDataObject(Schema!, FormConnector!);
            _form.FormLayout = Schema!.GetFormLayout();
            _form.DataObject = _dataObject;
            _listLayout = Schema.GetListLayout();
            // Columns render immediately; the rows arrive with the first ReloadListAsync.
            _grid.Bind(_listLayout, rows: null);
        }

        /// <summary>
        /// Resolves the <see cref="SystemApiConnector"/> used to load the
        /// <see cref="FormSchema"/>. Override to bypass <see cref="ClientInfo"/>.
        /// </summary>
        protected virtual SystemApiConnector? ResolveSystemConnector() => ClientInfo.SystemApiConnector;

        /// <summary>
        /// Resolves the <see cref="FormApiConnector"/> for CRUD round-trips.
        /// Override to bypass <see cref="ClientInfo"/>.
        /// </summary>
        protected virtual FormApiConnector ResolveFormConnector(string progId)
            => ClientInfo.CreateFormApiConnector(progId);

        /// <summary>
        /// Resolves the access token. Override to plug in a different session source.
        /// </summary>
        protected virtual Guid ResolveAccessToken() => ClientInfo.AccessToken;

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            // Fire-and-forget: the host attached us to a real visual tree, so trigger
            // the same async init the MAUI / Blazor sides run from their handler /
            // OnInitializedAsync hooks.
            if (!_initialized && !_isInitializing)
                _ = InitializeAsync();
        }

        private void OnInputsChanged()
        {
            if (_initialized || _isInitializing) return;
            _ = InitializeAsync();
        }

        private async Task ReloadListAsync()
        {
            var connector = FormConnector;
            if (connector is null) return;

            try
            {
                // FormSchema.ListFields drives the server-side SELECT fallback but does
                // not include sys_rowid; the framework only adds sys_rowid as an
                // invisible LayoutColumn on the client side. Explicitly prepend it to
                // SelectFields so the wire response carries the identifier the grid's
                // row-selection handler needs (without it row clicks silently drop on
                // the floor because TryGetRowId can't find the column).
                var response = await connector.GetListAsync(ComputeSelectFields()).ConfigureAwait(true);
                _grid.DataTable = response.Table;
                var isEmpty = response.Table is null || response.Table.Rows.Count == 0;
                _grid.IsVisible = !isEmpty;
                _emptyListLabel.IsVisible = isEmpty;
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }

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

        // Mode transitions sit on the success path of each guarded action — a thrown
        // round-trip leaves the form in its current mode.
        private async Task OnRowSelectedAsync(Guid rowId)
        {
            if (_dataObject is null) return;
            await RunGuardedAsync(async () =>
            {
                await _dataObject.LoadAsync(rowId).ConfigureAwait(true);
                FormMode = SingleFormMode.View;
            }).ConfigureAwait(true);
        }

        private void OnEditClicked()
        {
            if (_dataObject?.MasterRow is null) return;
            FormMode = SingleFormMode.Edit;
            UpdateToolbarState();
        }

        private async Task OnNewClickedAsync()
        {
            if (_dataObject is null) return;
            await RunGuardedAsync(async () =>
            {
                await _dataObject.NewAsync().ConfigureAwait(true);
                FormMode = SingleFormMode.Add;
            }).ConfigureAwait(true);
        }

        private async Task OnSaveClickedAsync()
        {
            if (_dataObject is null) return;
            await RunGuardedAsync(async () =>
            {
                await _dataObject.SaveAsync().ConfigureAwait(true);
                await ReloadListAsync().ConfigureAwait(true);
                FormMode = SingleFormMode.View;
            }).ConfigureAwait(true);
        }

        private async Task OnDeleteClickedAsync()
        {
            if (_dataObject is null) return;
            await RunGuardedAsync(async () =>
            {
                await _dataObject.DeleteAsync().ConfigureAwait(true);
                await ReloadListAsync().ConfigureAwait(true);
                FormMode = SingleFormMode.View;
            }).ConfigureAwait(true);
        }

        private async Task RunGuardedAsync(Func<Task> action)
        {
            if (_isBusy) return;
            _isBusy = true;
            ClearError();
            try
            {
                await action().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
            finally
            {
                _isBusy = false;
                UpdateToolbarState();
                RefreshFormView();
            }
        }

        private void RefreshFormView()
        {
            // FormDataObject mutates its DataSet in place across New/Load/Save/Delete,
            // so reassigning the same reference into DataObjectProperty would be a no-op
            // (StyledProperty change handlers only fire on reference changes). Drive
            // Rebuild explicitly instead.
            _form.Refresh();
        }

        /// <inheritdoc />
        protected override void OnFormModeChanged(SingleFormMode formMode)
        {
            UpdateToolbarState();
        }

        private void UpdateToolbarState()
        {
            var hasMaster = _dataObject?.MasterRow is not null;
            var browsing = FormMode == SingleFormMode.View;
            _newButton.IsEnabled = _initialized && !_isBusy;
            // Edit enters Edit mode from browsing; Save commits an Add/Edit session.
            _editButton.IsEnabled = _initialized && !_isBusy && hasMaster && browsing;
            _saveButton.IsEnabled = _initialized && !_isBusy && hasMaster && !browsing;
            _deleteButton.IsEnabled = _initialized && !_isBusy && hasMaster && browsing;
            _dirtyMarker.IsVisible = _dataObject?.IsDirty == true;
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
