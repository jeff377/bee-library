using Avalonia.Controls;
using Avalonia.Threading;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.DataObjects;
using Bee.UI.Avalonia.Permissions;
using Bee.UI.Core;
using Bee.UI.Core.Permissions;

namespace Bee.UI.Avalonia.Views
{
    /// <summary>
    /// Command handling and the data-object lifecycle behind it: save, close, toolbar state and live-computation callbacks.
    /// </summary>
    /// <remarks>
    /// Mirrors `ListView.Commands`. These are the paths a user's click reaches; the view's own layout and
    /// construction stay in the main file and `.Build`.
    /// </remarks>
    public partial class FormView
    {
        /// <summary>
        /// Ensures the data object exists and returns it, or <c>null</c> when the form cannot be
        /// initialized (no schema, no connector, or the schema lookup failed).
        /// </summary>
        /// <remarks>
        /// Returns the data object rather than a success flag so callers hold a non-null reference the
        /// compiler can verify. A <c>bool</c> would leave them dereferencing the field on trust — and
        /// that trust does not survive the lambda they pass to <c>RunGuardedAsync</c>.
        /// </remarks>
        private async Task<FormDataObject?> EnsureDataObjectAsync()
        {
            if (_dataObject is not null) return _dataObject;

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
                    return null;
                }
            }

            if (FormConnector is null && !string.IsNullOrEmpty(ProgId))
                FormConnector = ResolveFormConnector(ProgId);

            if (Schema is null || FormConnector is null) return null;

            _dataObject = new FormDataObject(Schema, FormConnector);
            // Live preview recomputes computed fields as the user edits. It is subscribed once because the
            // data object keeps these events across `DataSet` replacements on Load, New, and Save. The
            // rounding context (Tier 2 currency and unit masters plus company decimals) aligns previews to
            // the server, which still rounds authoritatively on save.
            _roundingContext = await ResolveRoundingContextAsync().ConfigureAwait(true);
            _liveComputation = new FormLiveComputation(Schema, _roundingContext);
            _dataObject.FieldValueChanged += OnLiveFieldValueChanged;
            _dataObject.RowAdded += OnLiveRowAdded;
            string layoutProgId = string.IsNullOrEmpty(ProgId) ? Schema.ProgId : ProgId;
            _formLayout = await ResolveLayoutAsync(layoutProgId).ConfigureAwait(true);
            // Degrade the layout against the cached capability snapshot before it
            // renders: hide sensitive fields without Read and mark them read-only without Update
            // (detail grid actions follow the form's edit mode, not permission). No-op when no
            // company context is active.
            LayoutCapabilityApplier.Apply(_formLayout, Schema, ClientInfo.Capabilities);
            return _dataObject;
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
