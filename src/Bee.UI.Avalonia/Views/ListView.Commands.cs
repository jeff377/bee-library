using System.Data;
using Avalonia.Controls;
using Bee.Definition;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Permissions;
using Bee.UI.Core;
using Bee.UI.Core.Permissions;

namespace Bee.UI.Avalonia.Views
{
    /// <summary>
    /// Grid-wiring and toolbar-command half of <see cref="ListView"/>. Split out for file size only;
    /// behaviour is unchanged.
    /// </summary>
    public partial class ListView
    {
        private void AttachGrid()
        {
            ClearError();
            _loadingLabel.IsVisible = false;

            var listLayout = Schema!.GetListLayout();
            // Degrade the freshly generated list layout before binding: sensitive columns the user
            // cannot Read drop out. No-op when no company context is active.
            LayoutCapabilityApplier.ApplyGrid(listLayout, Schema!, ClientInfo.Capabilities);
            // Columns render immediately; rows arrive with the first ReloadAsync.
            _grid.Bind(listLayout, rows: null);

            var columns = (listLayout.Columns ?? Enumerable.Empty<LayoutColumn>())
                .Where(c => c.Visible)
                .ToArray();
            _cardList.ItemTemplate = BuildCardTemplate(columns);
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

            // Capability gate (independent of selection/busy): hide a command the user cannot perform.
            _viewButton.IsVisible = CanCommand(_viewButton);
            _newButton.IsVisible = CanCommand(_newButton);
            _editButton.IsVisible = CanCommand(_editButton);
            _deleteButton.IsVisible = CanCommand(_deleteButton);
        }

        // Resolves whether the button's tagged PermissionAction is permitted for the current schema
        // and cached capability snapshot. Untagged buttons and a missing schema resolve to permitted.
        private bool CanCommand(Control button)
            => Schema is null
               || ElementCapabilityResolver.Default.Can(Schema, PermissionScope.GetAction(button), ClientInfo.Capabilities);

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
