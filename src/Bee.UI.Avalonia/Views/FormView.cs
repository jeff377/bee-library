using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Bee.Api.Client.Connectors;
using Bee.Base.Exceptions;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Definition.Settings;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;
using Bee.UI.Avalonia.Permissions;
using Bee.UI.Core;
using Bee.UI.Core.Permissions;

namespace Bee.UI.Avalonia.Views
{
    /// <summary>
    /// Single-record surface — the editor half of the ERP list/record split, paired with
    /// <see cref="ListView"/>. A record is one complete master + detail unit loaded through
    /// the form's <c>GetData</c> round-trip. The view renders the record from its
    /// <see cref="FormSchema.GetFormLayout"/> (master sections + detail grids) and carries
    /// the three <see cref="SingleFormMode"/> states: <see cref="SingleFormMode.View"/>
    /// (read-only, Back only), <see cref="SingleFormMode.Add"/> and
    /// <see cref="SingleFormMode.Edit"/> (editable, Save / Cancel). The host drives it through
    /// <see cref="ViewAsync"/> / <see cref="EditAsync"/> / <see cref="NewAsync"/> and reacts to
    /// <see cref="Saved"/> / <see cref="Closed"/> to return to the list.
    /// </summary>
    /// <remarks>
    /// The host typically supplies only <see cref="ProgId"/>; the schema, connector and access
    /// token resolve from <see cref="ClientInfo"/> through the <c>Resolve*</c> hooks, which a
    /// subclass can override to bypass the static state (the unit tests rely on this). Setting
    /// <see cref="FormMode"/> broadcasts through the ambient <see cref="FormScope"/> so the
    /// field editors and detail grids switch read-only / editable to match.
    /// </remarks>
    public partial class FormView : UserControl
    {
        /// <summary>Identifies the <see cref="ProgId"/> styled property.</summary>
        public static readonly StyledProperty<string> ProgIdProperty =
            AvaloniaProperty.Register<FormView, string>(nameof(ProgId), defaultValue: string.Empty);

        /// <summary>Identifies the <see cref="AccessToken"/> styled property.</summary>
        public static readonly StyledProperty<Guid> AccessTokenProperty =
            AvaloniaProperty.Register<FormView, Guid>(nameof(AccessToken), defaultValue: Guid.Empty);

        /// <summary>Identifies the <see cref="Schema"/> styled property.</summary>
        public static readonly StyledProperty<FormSchema?> SchemaProperty =
            AvaloniaProperty.Register<FormView, FormSchema?>(nameof(Schema));

        /// <summary>Identifies the <see cref="FormConnector"/> styled property.</summary>
        public static readonly StyledProperty<FormApiConnector?> FormConnectorProperty =
            AvaloniaProperty.Register<FormView, FormApiConnector?>(nameof(FormConnector));

        /// <summary>Identifies the <see cref="FormMode"/> styled property.</summary>
        public static readonly StyledProperty<SingleFormMode> FormModeProperty =
            AvaloniaProperty.Register<FormView, SingleFormMode>(nameof(FormMode), SingleFormMode.View);

        /// <summary>Identifies the <see cref="DetailEditMode"/> styled property.</summary>
        public static readonly StyledProperty<GridEditMode> DetailEditModeProperty =
            AvaloniaProperty.Register<FormView, GridEditMode>(nameof(DetailEditMode), GridEditMode.InCell);

        /// <summary>
        /// Default <see cref="CompactWidthThreshold"/> (DIPs): viewports narrower than this —
        /// phones, narrow windows — collapse master fields to a single column and switch detail
        /// grids to <see cref="GridEditMode.EditForm"/>.
        /// </summary>
        public const double DefaultCompactWidthThreshold = 600;

        /// <summary>Identifies the <see cref="CompactWidthThreshold"/> styled property.</summary>
        public static readonly StyledProperty<double> CompactWidthThresholdProperty =
            AvaloniaProperty.Register<FormView, double>(
                nameof(CompactWidthThreshold), DefaultCompactWidthThreshold);

        private readonly Button _saveButton;
        private readonly Button _cancelButton;
        private readonly Button _backButton;
        private readonly TextBlock _errorLabel;
        private readonly StackPanel _formHost;
        private FormDataObject? _dataObject;
        private FormLayout? _formLayout;
        // Client-side live recomputation of computed fields; created once with the data object.
        private FormLiveComputation? _liveComputation;
        // Reference-aware rounding context (Tier 2) resolved once at init: rounds computed previews and
        // formats amount/quantity cells to the same decimals the server uses. Empty = framework defaults.
        private RoundingContext _roundingContext = new();
        // Detail grids by table name, repopulated on every Rebuild so a live recompute of a detail
        // row can refresh the matching grid (realized cells do not track later DataRow writes).
        private readonly Dictionary<string, GridControl> _detailGrids = new(StringComparer.OrdinalIgnoreCase);
        private bool _isBusy;
        // Last applied compact-layout state. Crossing the width threshold rebuilds the form so
        // master fields reflow (multi-column <-> single) and detail grids re-render in the
        // matching edit model; a plain Bounds tick that does not cross the threshold is a no-op.
        private bool _isCompact;

        static FormView()
        {
            FormModeProperty.Changed.AddClassHandler<FormView>((v, e) =>
            {
                var formMode = (SingleFormMode)e.NewValue!;
                FormScope.SetFormMode(v, formMode);
                v.OnFormModeChanged(formMode);
            });
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FormView"/> with the toolbar + form body
        /// assembled. The toolbar reflects the current <see cref="FormMode"/>.
        /// </summary>
        public FormView()
        {
            // The ambient scope defaults to Edit so standalone editors stay editable; a data
            // form owns the mode, so pin the scope to the initial View here (the property
            // change handler cannot cover this — the default value never raises a change).
            FormScope.SetFormMode(this, FormMode);

            _errorLabel = new TextBlock { Foreground = Brushes.Red, IsVisible = false };

            _saveButton = new Button { Content = "Save" };
            // Save persists via Create (Add mode) or Update (Edit mode); any-of semantics show it
            // when the user holds either. Cancel / Back are navigation, not permission-controlled.
            PermissionScope.SetAction(_saveButton, PermissionAction.Create | PermissionAction.Update);
            _saveButton.Click += async (_, _) => await OnSaveClickedAsync().ConfigureAwait(true);

            _cancelButton = new Button { Content = "Cancel" };
            _cancelButton.Click += (_, _) => OnCloseClicked();

            _backButton = new Button { Content = "Back" };
            _backButton.Click += (_, _) => OnCloseClicked();

            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            toolbar.Children.Add(_saveButton);
            toolbar.Children.Add(_cancelButton);
            toolbar.Children.Add(_backButton);

            _formHost = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };

            // Error label and toolbar stay pinned at the top so the `Save`, `Cancel` and `Back`
            // buttons remain reachable, while the form body scrolls. Without this a tall
            // single-column (compact) layout overflows the viewport with no way to reach the
            // controls below the fold.
            var topBar = new StackPanel { Orientation = Orientation.Vertical, Spacing = 12 };
            topBar.Children.Add(_errorLabel);
            topBar.Children.Add(toolbar);
            DockPanel.SetDock(topBar, Dock.Top);

            var scroller = new ScrollViewer
            {
                Content = _formHost,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 12, 0, 0),
            };

            var host = new DockPanel();
            host.Children.Add(topBar);
            host.Children.Add(scroller);

            Content = host;
            UpdateToolbarState();
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
        /// <see cref="Guid.Empty"/>; resolves from <see cref="ClientInfo.AccessToken"/> when
        /// the host did not supply one.
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

        /// <summary>Gets or sets the connector used for the load / save round-trips.</summary>
        public FormApiConnector? FormConnector
        {
            get => GetValue(FormConnectorProperty);
            set => SetValue(FormConnectorProperty, value);
        }

        /// <summary>
        /// Gets or sets the form mode. Defaults to <see cref="SingleFormMode.View"/>; every
        /// change is broadcast to descendant editors and grids through the ambient
        /// <see cref="FormScope"/>.
        /// </summary>
        public SingleFormMode FormMode
        {
            get => GetValue(FormModeProperty);
            set => SetValue(FormModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the preferred detail-grid editing model for wide (desktop) viewports.
        /// On viewports narrower than <see cref="CompactWidthThreshold"/> this is overridden by
        /// <see cref="GridEditMode.EditForm"/> so phone-sized layouts edit rows in a form rather
        /// than in-cell. Defaults to <see cref="GridEditMode.InCell"/>.
        /// </summary>
        public GridEditMode DetailEditMode
        {
            get => GetValue(DetailEditModeProperty);
            set => SetValue(DetailEditModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the viewport width (DIPs) at or above which detail grids honour
        /// <see cref="DetailEditMode"/>. Below it the grids switch to
        /// <see cref="GridEditMode.EditForm"/> regardless of <see cref="DetailEditMode"/>.
        /// Defaults to <see cref="DefaultCompactWidthThreshold"/>.
        /// </summary>
        public double CompactWidthThreshold
        {
            get => GetValue(CompactWidthThresholdProperty);
            set => SetValue(CompactWidthThresholdProperty, value);
        }

        /// <summary>Gets the data object built once the schema + connector are resolved.</summary>
        public FormDataObject? DataObject => _dataObject;

        /// <summary>Raised after a record is saved successfully.</summary>
        public event EventHandler? Saved;

        /// <summary>
        /// Raised when the surface is dismissed without saving — Cancel (Add / Edit) or Back
        /// (View). The host returns to the list.
        /// </summary>
        public event EventHandler? Closed;

        /// <summary>Raised whenever a backend round-trip throws.</summary>
        public event EventHandler<Exception>? ErrorOccurred;

        /// <summary>Loads a record read-only (<see cref="SingleFormMode.View"/>).</summary>
        public async Task ViewAsync(Guid rowId)
        {
            if (!await EnsureInitializedAsync().ConfigureAwait(true)) return;
            await RunGuardedAsync(async () =>
            {
                await _dataObject!.LoadAsync(rowId).ConfigureAwait(true);
                FormMode = SingleFormMode.View;
            }).ConfigureAwait(true);
        }

        /// <summary>Loads a record for editing (<see cref="SingleFormMode.Edit"/>).</summary>
        public async Task EditAsync(Guid rowId)
        {
            if (!await EnsureInitializedAsync().ConfigureAwait(true)) return;
            await RunGuardedAsync(async () =>
            {
                await _dataObject!.LoadAsync(rowId).ConfigureAwait(true);
                FormMode = SingleFormMode.Edit;
            }).ConfigureAwait(true);
        }

        /// <summary>Starts a blank record (<see cref="SingleFormMode.Add"/>).</summary>
        public async Task NewAsync()
        {
            if (!await EnsureInitializedAsync().ConfigureAwait(true)) return;
            await RunGuardedAsync(async () =>
            {
                await _dataObject!.NewAsync().ConfigureAwait(true);
                // The server's `GetNewData` seeds columns but does not evaluate the
                // `DefaultValueExpression`. Apply the display-layer defaults (and recompute) so the blank
                // master row shows them at once. The master row does not raise `RowAdded` because it is
                // populated before the event bridge attaches, so seed it explicitly here.
                var master = _dataObject.MasterRow;
                if (master is not null)
                    _liveComputation?.InitializeNewRow(_dataObject.MasterTable.TableName, master);
                FormMode = SingleFormMode.Add;
            }).ConfigureAwait(true);
        }

        /// <summary>
        /// Resolves the <see cref="FormSchema"/> for <paramref name="progId"/> when the host did
        /// not pre-set <see cref="Schema"/>. Defaults to the cached <see cref="ClientInfo.DefineAccess"/>;
        /// override to supply a schema without touching the static <see cref="ClientInfo"/>.
        /// </summary>
        protected virtual async Task<FormSchema?> ResolveSchemaAsync(string progId)
            => await ClientInfo.DefineAccess.GetFormSchemaAsync(progId).ConfigureAwait(false);

        /// <summary>
        /// Resolves the <see cref="FormApiConnector"/> for the load / save round-trips.
        /// Override to bypass <see cref="ClientInfo"/>.
        /// </summary>
        protected virtual FormApiConnector ResolveFormConnector(string progId)
            => ClientInfo.CreateFormApiConnector(progId);

        /// <summary>Resolves the access token. Override to plug in a different session source.</summary>
        protected virtual Guid ResolveAccessToken() => ClientInfo.AccessToken;

        /// <summary>
        /// Resolves the rounding context used to round live-preview computed fields and format
        /// amount/quantity cells (Tier 2). The default pulls the currency/unit masters through the cached
        /// <see cref="ClientInfo.DefineAccess"/> and the company from <see cref="ClientInfo.Company"/>;
        /// each part is optional and degrades to framework-default decimal places when absent. Override
        /// to supply a context without touching the static <see cref="ClientInfo"/> (the unit tests do).
        /// </summary>
        protected virtual async Task<RoundingContext> ResolveRoundingContextAsync()
        {
            return new RoundingContext
            {
                Company = ClientInfo.Company,
                CurrencySettings = await TryResolveSettingAsync(ClientInfo.DefineAccess.GetCurrencySettingsAsync)
                    .ConfigureAwait(true),
                UnitSettings = await TryResolveSettingAsync(ClientInfo.DefineAccess.GetUnitSettingsAsync)
                    .ConfigureAwait(true),
            };
        }

        // Best-effort fetch of an optional definition master: a missing master already returns null, and
        // a permission/API error must not break the form — live preview simply falls back to
        // framework-default decimals for that kind (the server still rounds authoritatively on save).
        private static async Task<T?> TryResolveSettingAsync<T>(Func<Task<T>> fetch) where T : class
        {
            try
            {
                return await fetch().ConfigureAwait(true);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (ForbiddenException)
            {
                return null;
            }
        }

        /// <summary>
        /// Called after the form mode changed and was broadcast to the scope. Refreshes the
        /// mode-dependent toolbar.
        /// </summary>
        /// <param name="formMode">The new form mode.</param>
        protected virtual void OnFormModeChanged(SingleFormMode formMode) => UpdateToolbarState();

        private async Task<bool> EnsureInitializedAsync()
        {
            if (_dataObject is not null) return true;

            ApplyAccessTokenFallback();

            if (Schema is null && !string.IsNullOrEmpty(ProgId))
            {
                try
                {
                    var loaded = await ResolveSchemaAsync(ProgId).ConfigureAwait(true);
                    if (loaded is not null)
                        Schema = loaded;
                }
                catch (Exception ex)
                {
                    ReportError(ex);
                    return false;
                }
            }

            if (FormConnector is null && !string.IsNullOrEmpty(ProgId))
                FormConnector = ResolveFormConnector(ProgId);

            if (Schema is null || FormConnector is null) return false;

            _dataObject = new FormDataObject(Schema, FormConnector);
            // Live preview recomputes computed fields as the user edits. It is subscribed once because the
            // data object keeps these events across `DataSet` replacements on Load, New, and Save. The
            // rounding context (Tier 2 currency and unit masters plus company decimals) aligns previews to
            // the server, which still rounds authoritatively on save.
            _roundingContext = await ResolveRoundingContextAsync().ConfigureAwait(true);
            _liveComputation = new FormLiveComputation(Schema, _roundingContext);
            _dataObject.FieldValueChanged += OnLiveFieldValueChanged;
            _dataObject.RowAdded += OnLiveRowAdded;
            _formLayout = Schema.GetFormLayout();
            // Degrade the freshly generated layout against the cached capability snapshot before it
            // renders: hide sensitive fields without Read and mark them read-only without Update
            // (detail grid actions follow the form's edit mode, not permission). No-op when no
            // company context is active.
            LayoutCapabilityApplier.Apply(_formLayout, Schema, ClientInfo.Capabilities);
            return true;
        }

        private void ApplyAccessTokenFallback()
        {
            if (AccessToken != Guid.Empty) return;
            var fallbackToken = ResolveAccessToken();
            if (fallbackToken != Guid.Empty)
                AccessToken = fallbackToken;
        }

        private async Task OnSaveClickedAsync()
        {
            if (_dataObject is null) return;
            // RunGuardedAsync reports whether the action completed, rather than the action
            // mutating a captured local — the latter defeats the analyzer's data-flow
            // tracking through the closure and trips a false "always false" on the check below.
            var saved = await RunGuardedAsync(
                () => _dataObject.SaveAsync()).ConfigureAwait(true);

            if (saved)
                Saved?.Invoke(this, EventArgs.Empty);
        }

        private void OnCloseClicked()
        {
            if (_isBusy) return;
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private async Task<bool> RunGuardedAsync(Func<Task> action)
        {
            if (_isBusy) return false;
            _isBusy = true;
            ClearError();
            var completed = false;
            try
            {
                await action().ConfigureAwait(true);
                completed = true;
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
            finally
            {
                _isBusy = false;
                Rebuild();
                UpdateToolbarState();
            }
            return completed;
        }

        private void UpdateToolbarState()
        {
            var editing = FormMode != SingleFormMode.View;
            // Visibility combines the form mode with the command capability: a button hidden by
            // mode stays hidden, and a mode-visible button is further hidden when the user lacks
            // its permission action (Cancel / Back are untagged, so capability leaves them alone).
            _saveButton.IsVisible = editing && CanCommand(_saveButton);
            _cancelButton.IsVisible = editing && CanCommand(_cancelButton);
            _backButton.IsVisible = !editing && CanCommand(_backButton);

            _saveButton.IsEnabled = editing && !_isBusy && _dataObject?.MasterRow is not null;
        }

        // Resolves whether the button's tagged PermissionAction is permitted for the current schema
        // and cached capability snapshot. Untagged buttons (Action == None) and a missing schema
        // resolve to permitted, so this only ever hides a genuinely un-permitted command.
        private bool CanCommand(Control button)
            => Schema is null
               || ElementCapabilityResolver.Default.Can(Schema, PermissionScope.GetAction(button), ClientInfo.Capabilities);

        // ---- Live recomputation of computed fields ----

        /// <summary>
        /// Recomputes the edited row's computed fields on every field change. The write-backs re-raise
        /// <see cref="FormDataObject.FieldValueChanged"/>; the live-computation guard makes this handler a
        /// no-op for those echoes, so a single edit yields one recompute pass. Master field editors
        /// re-pull through their own subscription; a detail grid's realized cells do not, so the matching
        /// grid is refreshed.
        /// </summary>
        private void OnLiveFieldValueChanged(object? sender, FieldValueChangedEventArgs e)
        {
            if (_liveComputation is null || _liveComputation.IsRecomputing) { return; }
            var changed = _liveComputation.Recompute(e.TableName, e.FieldName, e.Row);
            if (changed.Count > 0)
                RefreshDetailGrid(e.TableName);
        }

        /// <summary>
        /// Initializes a newly added detail row: fills its default-value expressions and computes its
        /// computed fields so it renders complete. The master row is seeded in <see cref="NewAsync"/>
        /// instead (it is populated before the event bridge attaches and raises no <c>RowAdded</c>).
        /// </summary>
        private void OnLiveRowAdded(object? sender, RowChangedEventArgs e)
        {
            if (_liveComputation is null) { return; }
            var changed = _liveComputation.InitializeNewRow(e.TableName, e.Row);
            if (changed.Count > 0)
                RefreshDetailGrid(e.TableName);
        }

        /// <summary>
        /// Refreshes the detail grid bound to <paramref name="tableName"/>, if one is rendered. Master
        /// changes need no refresh (field editors re-pull themselves). Posted to the dispatcher so the
        /// refresh runs after the current cell-edit commit unwinds, not re-entering the grid's edit
        /// pipeline.
        /// </summary>
        private void RefreshDetailGrid(string tableName)
        {
            if (_dataObject is not null &&
                string.Equals(tableName, _dataObject.MasterTable.TableName, StringComparison.OrdinalIgnoreCase))
                return;
            if (!_detailGrids.TryGetValue(tableName, out var grid)) { return; }
            Dispatcher.UIThread.Post(grid.RefreshRows, DispatcherPriority.Background);
        }
    }
}
