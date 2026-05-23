using Bee.Api.Client.Connectors;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Core;
using Bee.UI.Maui.DataObjects;

namespace Bee.UI.Maui.Controls
{
    /// <summary>
    /// MAUI <see cref="ContentView"/> that wires <see cref="DynamicGrid"/> (list view)
    /// to <see cref="DynamicForm"/> (master detail) via a shared
    /// <see cref="FormDataObject"/>. Selecting a list row drives
    /// <see cref="FormDataObject.LoadAsync"/>; the toolbar buttons fan out to
    /// <see cref="FormDataObject.NewAsync"/> / <c>SaveAsync</c> / <c>DeleteAsync</c>.
    /// Mirrors the Blazor <c>FormPage</c> component structure for cross-family parity.
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
    public class FormPage : ContentView
    {
        /// <summary>
        /// Identifies the <see cref="ProgId"/> bindable property.
        /// </summary>
        public static readonly BindableProperty ProgIdProperty = BindableProperty.Create(
            nameof(ProgId),
            typeof(string),
            typeof(FormPage),
            defaultValue: string.Empty);

        /// <summary>
        /// Identifies the <see cref="AccessToken"/> bindable property.
        /// </summary>
        public static readonly BindableProperty AccessTokenProperty = BindableProperty.Create(
            nameof(AccessToken),
            typeof(Guid),
            typeof(FormPage),
            defaultValue: Guid.Empty);

        /// <summary>
        /// Identifies the <see cref="Schema"/> bindable property.
        /// </summary>
        public static readonly BindableProperty SchemaProperty = BindableProperty.Create(
            nameof(Schema),
            typeof(FormSchema),
            typeof(FormPage),
            propertyChanged: (b, _, _) => ((FormPage)b).OnInputsChanged());

        /// <summary>
        /// Identifies the <see cref="FormConnector"/> bindable property.
        /// </summary>
        public static readonly BindableProperty FormConnectorProperty = BindableProperty.Create(
            nameof(FormConnector),
            typeof(FormApiConnector),
            typeof(FormPage),
            propertyChanged: (b, _, _) => ((FormPage)b).OnInputsChanged());

        private readonly Button _newButton;
        private readonly Button _saveButton;
        private readonly Button _deleteButton;
        private readonly Label _dirtyMarker;
        private readonly Label _errorLabel;
        private readonly Label _loadingLabel;
        private readonly DynamicGrid _grid;
        private readonly DynamicForm _form;
        private FormDataObject? _dataObject;
        private bool _isBusy;
        private bool _initialized;
        private bool _isInitializing;

        /// <summary>
        /// Initializes a new instance of <see cref="FormPage"/> with the toolbar +
        /// list + form layout assembled. The toolbar buttons are disabled until
        /// <see cref="InitializeAsync"/> resolves the schema and connector inputs.
        /// </summary>
        public FormPage()
        {
            _errorLabel = new Label { TextColor = Colors.Red, IsVisible = false };
            _loadingLabel = new Label { Text = "Loading…", IsVisible = true };

            _newButton = new Button { Text = "New", IsEnabled = false };
            _newButton.Clicked += async (_, _) => await OnNewClickedAsync().ConfigureAwait(true);

            _saveButton = new Button { Text = "Save", IsEnabled = false };
            _saveButton.Clicked += async (_, _) => await OnSaveClickedAsync().ConfigureAwait(true);

            _deleteButton = new Button { Text = "Delete", IsEnabled = false };
            _deleteButton.Clicked += async (_, _) => await OnDeleteClickedAsync().ConfigureAwait(true);

            _dirtyMarker = new Label
            {
                Text = "● unsaved",
                TextColor = Colors.DarkGoldenrod,
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false,
            };

            var toolbar = new HorizontalStackLayout
            {
                Spacing = 8,
                Children = { _newButton, _saveButton, _deleteButton, _dirtyMarker },
            };

            _grid = new DynamicGrid();
            _grid.RowSelected += async (_, rowId) => await OnRowSelectedAsync(rowId).ConfigureAwait(true);

            _form = new DynamicForm();

            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children = { _errorLabel, _loadingLabel, toolbar, _grid, _form },
            };
        }

        /// <summary>
        /// Gets or sets the program identifier (e.g. "Employee"). Drives both the
        /// <see cref="ClientInfo"/>-backed FormSchema lookup and the
        /// <see cref="FormApiConnector"/> creation when the host leaves
        /// <see cref="Schema"/> / <see cref="FormConnector"/> unset.
        /// </summary>
        public string ProgId
        {
            get => (string)GetValue(ProgIdProperty);
            set => SetValue(ProgIdProperty, value);
        }

        /// <summary>
        /// Gets or sets the access token surfaced on the page. Defaults to
        /// <see cref="Guid.Empty"/> (anonymous); <see cref="InitializeAsync"/>
        /// fills this in from <see cref="ClientInfo.AccessToken"/> when the host
        /// did not supply one. Note that a host-supplied <see cref="FormConnector"/>
        /// keeps the token it was constructed with — changing the page's
        /// <see cref="AccessToken"/> does not redirect that connector.
        /// </summary>
        public Guid AccessToken
        {
            get => (Guid)GetValue(AccessTokenProperty);
            set => SetValue(AccessTokenProperty, value);
        }

        /// <summary>
        /// Gets or sets the resolved <see cref="FormSchema"/>. When this and
        /// <see cref="FormConnector"/> are both set the page builds its
        /// <see cref="FormDataObject"/> and enables the toolbar.
        /// </summary>
        public FormSchema? Schema
        {
            get => (FormSchema?)GetValue(SchemaProperty);
            set => SetValue(SchemaProperty, value);
        }

        /// <summary>
        /// Gets or sets the connector used for all CRUD round-trips
        /// (<c>GetList</c> / <c>GetData</c> / <c>GetNewData</c> / <c>Save</c> / <c>Delete</c>).
        /// </summary>
        public FormApiConnector? FormConnector
        {
            get => (FormApiConnector?)GetValue(FormConnectorProperty);
            set => SetValue(FormConnectorProperty, value);
        }

        /// <summary>
        /// Raised whenever a backend round-trip throws, so the host can surface
        /// the error in its own status bar. The exception is also rendered inline
        /// in the page's <see cref="_errorLabel"/>.
        /// </summary>
        public event EventHandler<Exception>? ErrorOccurred;

        /// <summary>
        /// Gets the data object built once <see cref="Schema"/> and
        /// <see cref="FormConnector"/> are both supplied. Exposed for tests and
        /// host inspection; production callers should drive the page through the
        /// toolbar buttons rather than reaching in here.
        /// </summary>
        public FormDataObject? DataObject => _dataObject;

        /// <summary>
        /// Builds <see cref="FormDataObject"/> from the current <see cref="Schema"/>
        /// + <see cref="FormConnector"/> and runs the initial list reload. When
        /// either is missing, the page resolves the gap from <see cref="ClientInfo"/>
        /// via the <c>ResolveSystemConnector</c> / <c>ResolveFormConnector</c> /
        /// <c>ResolveAccessToken</c> hooks. The call is a no-op while a previous
        /// invocation is still running and after the first successful initialization,
        /// mirroring the lazy attach behaviour of the Blazor <c>OnInitializedAsync</c>
        /// hook.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized || _isInitializing) return;

            // We can move forward when ProgId is set (so we can resolve via ClientInfo)
            // or when the host has already supplied both Schema and FormConnector. Any
            // other state leaves nothing to initialize.
            var hasProgId = !string.IsNullOrEmpty(ProgId);
            if (!hasProgId && (Schema is null || FormConnector is null)) return;

            _isInitializing = true;
            try
            {
                // AccessToken fallback: ClientInfo holds the session token issued at
                // login; surface it on the page property so hosts can observe the
                // resolved value (the connectors created below already capture it).
                if (AccessToken == Guid.Empty)
                {
                    var fallbackToken = ResolveAccessToken();
                    if (fallbackToken != Guid.Empty)
                        AccessToken = fallbackToken;
                }

                if (Schema is null && hasProgId)
                {
                    var systemConnector = ResolveSystemConnector();
                    if (systemConnector is not null)
                    {
                        ClearError();
                        try
                        {
                            var loaded = await systemConnector
                                .GetDefineAsync<FormSchema>(DefineType.FormSchema, new[] { ProgId })
                                .ConfigureAwait(true);
                            if (loaded is not null)
                                Schema = loaded;
                        }
                        catch (Exception ex)
                        {
                            ReportError(ex);
                            return;
                        }
                    }
                }

                if (FormConnector is null && hasProgId)
                {
                    FormConnector = ResolveFormConnector(ProgId);
                }

                if (Schema is null || FormConnector is null) return;

                ClearError();
                _loadingLabel.IsVisible = false;

                var formLayout = Schema.GetFormLayout();
                var listLayout = Schema.GetListLayout();
                _dataObject = new FormDataObject(Schema, FormConnector);

                _form.FormLayout = formLayout;
                _form.DataObject = _dataObject;
                _grid.ListLayout = listLayout;

                _initialized = true;
                await ReloadListAsync().ConfigureAwait(true);
                UpdateToolbarState();
            }
            finally
            {
                _isInitializing = false;
            }
        }

        /// <summary>
        /// Resolves the <see cref="SystemApiConnector"/> used to load the
        /// <see cref="FormSchema"/> when the host did not pre-set <see cref="Schema"/>.
        /// Defaults to <see cref="ClientInfo.SystemApiConnector"/>; tests and hosts
        /// that need to bypass the static <see cref="ClientInfo"/> can override.
        /// </summary>
        protected virtual SystemApiConnector? ResolveSystemConnector() => ClientInfo.SystemApiConnector;

        /// <summary>
        /// Resolves the <see cref="FormApiConnector"/> used for CRUD round-trips
        /// when the host did not pre-set <see cref="FormConnector"/>. Defaults to
        /// <see cref="ClientInfo.CreateFormApiConnector(string)"/>; tests and hosts
        /// that need to bypass the static <see cref="ClientInfo"/> can override.
        /// </summary>
        /// <param name="progId">Program identifier the connector targets.</param>
        protected virtual FormApiConnector ResolveFormConnector(string progId)
            => ClientInfo.CreateFormApiConnector(progId);

        /// <summary>
        /// Resolves the access token surfaced through <see cref="AccessToken"/> when
        /// the host did not provide one. Defaults to <see cref="ClientInfo.AccessToken"/>;
        /// override to plug in a different session source.
        /// </summary>
        protected virtual Guid ResolveAccessToken() => ClientInfo.AccessToken;

        /// <inheritdoc/>
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            // Fire-and-forget: the host attached us to a real visual tree, so trigger
            // the same async init the Blazor side runs from OnInitializedAsync. If
            // initialization can't complete yet (e.g. ProgId still empty), the call
            // no-ops and the next input change will retry through OnInputsChanged.
            if (!_initialized && !_isInitializing)
                _ = InitializeAsync();
        }

        private void OnInputsChanged()
        {
            // _isInitializing is checked because InitializeAsync writes back to
            // Schema / FormConnector after resolving them from ClientInfo, which
            // re-triggers this callback. The outer InitializeAsync is still on the
            // stack, so re-entering would race against its own state.
            if (_initialized || _isInitializing) return;
            _ = InitializeAsync();
        }

        private async Task ReloadListAsync()
        {
            var connector = FormConnector;
            if (connector is null) return;

            try
            {
                var response = await connector.GetListAsync().ConfigureAwait(true);
                _grid.Rows = response.Table;
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }

        private async Task OnRowSelectedAsync(Guid rowId)
        {
            if (_dataObject is null) return;
            await RunGuardedAsync(() => _dataObject.LoadAsync(rowId)).ConfigureAwait(true);
        }

        private async Task OnNewClickedAsync()
        {
            if (_dataObject is null) return;
            await RunGuardedAsync(_dataObject.NewAsync).ConfigureAwait(true);
        }

        private async Task OnSaveClickedAsync()
        {
            if (_dataObject is null) return;
            await RunGuardedAsync(async () =>
            {
                await _dataObject.SaveAsync().ConfigureAwait(true);
                await ReloadListAsync().ConfigureAwait(true);
            }).ConfigureAwait(true);
        }

        private async Task OnDeleteClickedAsync()
        {
            if (_dataObject is null) return;
            await RunGuardedAsync(async () =>
            {
                await _dataObject.DeleteAsync().ConfigureAwait(true);
                await ReloadListAsync().ConfigureAwait(true);
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
            // Reassign DataObject so the DynamicForm rebuilds its inputs against
            // the (possibly replaced) DataSet returned by the connector.
            _form.DataObject = _dataObject;
        }

        private void UpdateToolbarState()
        {
            var hasMaster = _dataObject?.MasterRow is not null;
            _newButton.IsEnabled = _initialized && !_isBusy;
            _saveButton.IsEnabled = _initialized && !_isBusy && hasMaster;
            _deleteButton.IsEnabled = _initialized && !_isBusy && hasMaster;
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
