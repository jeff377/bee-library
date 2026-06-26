using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Bee.Api.Client.Connectors;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;
using Bee.UI.Core;

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
    public class FormView : UserControl
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
            _formLayout = Schema.GetFormLayout();
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
            _saveButton.IsVisible = editing;
            _cancelButton.IsVisible = editing;
            _backButton.IsVisible = !editing;

            _saveButton.IsEnabled = editing && !_isBusy && _dataObject?.MasterRow is not null;
        }

        // ---- Record rendering (master sections + detail grids) ----

        private void Rebuild()
        {
            _formHost.Children.Clear();
            if (_formLayout is null || _dataObject is null) return;

            // Build against the current width so the first render (which may run before any Bounds
            // notification) already reflects the compact state; resize crossings re-enter via
            // ApplyResponsiveState.
            _isCompact = IsCompactWidth(GetViewportWidth(), CompactWidthThreshold);

            foreach (var section in EnumerateSections())
                _formHost.Children.Add(BuildSection(section));
            foreach (var detail in EnumerateDetails())
                _formHost.Children.Add(BuildDetailSection(detail));
        }

        private Border BuildSection(LayoutSection section)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
            if (section.ShowCaption && !string.IsNullOrEmpty(section.Caption))
            {
                stack.Children.Add(new TextBlock { Text = section.Caption, FontWeight = FontWeight.Bold });
            }

            stack.Children.Add(BuildFieldGrid(section));

            return new Border
            {
                Padding = new Thickness(8),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Child = stack,
            };
        }

        private Grid BuildFieldGrid(LayoutSection section)
        {
            var columnCount = EffectiveColumnCount();
            var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
            for (int i = 0; i < columnCount; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            int row = 0, col = 0;
            foreach (var field in EnumerateFields(section))
            {
                var (rowSpan, colSpan) = NormalizeSpans(field);

                // CSS-grid-like wrap: if the field would overflow the row, advance first.
                if (col + colSpan > columnCount)
                {
                    row++;
                    col = 0;
                }

                while (grid.RowDefinitions.Count < row + rowSpan)
                    grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                var cell = BuildFieldCell(field);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                Grid.SetRowSpan(cell, rowSpan);
                Grid.SetColumnSpan(cell, colSpan);
                grid.Children.Add(cell);

                col += colSpan;
                if (col >= columnCount)
                {
                    row++;
                    col = 0;
                }
            }

            return grid;
        }

        private StackPanel BuildFieldCell(LayoutField field)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
            var caption = new TextBlock { Text = field.Caption };
            // A required field's caption is blue; a read-only field's caption stays the theme
            // default — its cue is the editor's read-only underline (the detail grid, which has no
            // such per-cell visual, parenthesises the header instead). See FieldCaptionStyle.
            if (Controls.FieldCaptionStyle.GetCaptionForeground(field.ReadOnly, field.Required) is { } brush)
                caption.Foreground = brush;
            stack.Children.Add(caption);
            stack.Children.Add(BuildInputControl(field));
            return stack;
        }

        // Dispatches LayoutField.ControlType to the corresponding field editor; the editor
        // pulls its value, applies FormField metadata and refreshes through FormDataObject.
        private Control BuildInputControl(LayoutField field)
        {
            var editor = FieldEditorFactory.Create(field.ControlType);
            ((IFieldEditor)editor).Bind(_dataObject!, field);
            return editor;
        }

        private Border BuildDetailSection(LayoutGrid layout)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 4 };
            if (!string.IsNullOrEmpty(layout.Caption))
            {
                stack.Children.Add(new TextBlock { Text = layout.Caption, FontWeight = FontWeight.Bold });
            }

            // The grid carries its own icon toolbar and edit-form flow; the form only supplies the
            // layout, the data object and the editing model (responsive — see EffectiveDetailEditMode).
            var grid = new GridControl { MinHeight = 120, EditMode = EffectiveDetailEditMode() };
            grid.Bind(_dataObject!, layout);
            stack.Children.Add(grid);

            return new Border
            {
                Padding = new Thickness(8),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gray,
                Child = stack,
            };
        }

        // ---- Responsive layout (compact = phone-sized viewport) ----

        /// <summary>
        /// Reacts to the inputs of the responsive layout decision. <see cref="Visual.Bounds"/> is
        /// a direct property whose change notifications are not delivered to static class handlers,
        /// so the width reaction is wired here (the same pattern OverlayDialogHost uses) rather than
        /// in the static constructor.
        /// </summary>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == BoundsProperty || change.Property == CompactWidthThresholdProperty)
            {
                ApplyResponsiveState();
            }
            else if (change.Property == DetailEditModeProperty && !_isCompact)
            {
                // The preferred detail mode only takes effect on wide layouts; a compact layout
                // forces EditForm regardless, so only a wide form needs to re-render here.
                RebuildIfReady();
            }
        }

        /// <summary>
        /// Pure decision: a positive width below <paramref name="compactWidthThreshold"/> is a
        /// compact (phone-sized) viewport. A non-positive width means "not measured yet", so the
        /// layout stays in its wide form until the first layout pass.
        /// </summary>
        internal static bool IsCompactWidth(double viewportWidth, double compactWidthThreshold)
            => viewportWidth > 0 && viewportWidth < compactWidthThreshold;

        /// <summary>
        /// Gets the viewport width used for the compact-layout decision. Defaults to the
        /// control's own <see cref="Visual.Bounds"/> width; overridden in tests to drive the
        /// responsive switch without a real layout pass.
        /// </summary>
        protected virtual double GetViewportWidth() => Bounds.Width;

        /// <summary>
        /// Recomputes the compact state from the current viewport width and rebuilds the form only
        /// when the <see cref="CompactWidthThreshold"/> boundary is crossed, so the frequent
        /// within-band <see cref="Visual.Bounds"/> ticks during layout cost a single comparison.
        /// </summary>
        protected void ApplyResponsiveState()
        {
            var compact = IsCompactWidth(GetViewportWidth(), CompactWidthThreshold);
            if (compact == _isCompact) return;
            _isCompact = compact;
            RebuildIfReady();
        }

        private void RebuildIfReady()
        {
            if (_formLayout is not null && _dataObject is not null)
                Rebuild();
        }

        // Master fields collapse to a single column on a compact viewport; otherwise the layout's
        // own column count applies.
        private int EffectiveColumnCount()
            => _isCompact ? 1 : NormalizeColumnCount(_formLayout?.ColumnCount);

        // Detail grids edit in a form on a compact viewport; otherwise the preferred mode applies.
        private GridEditMode EffectiveDetailEditMode()
            => _isCompact ? GridEditMode.EditForm : DetailEditMode;

        private IEnumerable<LayoutSection> EnumerateSections()
            => _formLayout?.Sections ?? Enumerable.Empty<LayoutSection>();

        private IEnumerable<LayoutGrid> EnumerateDetails()
            => _formLayout?.Details ?? Enumerable.Empty<LayoutGrid>();

        private static IEnumerable<LayoutField> EnumerateFields(LayoutSection section)
            => section.Fields?.Where(f => f.Visible) ?? Enumerable.Empty<LayoutField>();

        private static int NormalizeColumnCount(int? columnCount)
        {
            var n = columnCount ?? 1;
            return n < 1 ? 1 : n;
        }

        private static (int rowSpan, int columnSpan) NormalizeSpans(LayoutField field)
        {
            var rowSpan = field.RowSpan < 1 ? 1 : field.RowSpan;
            var colSpan = field.ColumnSpan < 1 ? 1 : field.ColumnSpan;
            return (rowSpan, colSpan);
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
