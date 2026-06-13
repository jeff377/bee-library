using System.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Bee.Api.Client.Connectors;
using Bee.Definition.Forms;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// The selection surface of a lookup picker: a search box, a
    /// <see cref="GridControl"/> bound to the target form's lookup layout
    /// (see <c>FormSchema.GetLookupLayout</c>), and OK / Cancel buttons.
    /// Rows come from <see cref="FormApiConnector.GetLookupAsync"/>, so the
    /// projection and search semantics are server-resolved.
    /// </summary>
    /// <remarks>
    /// Presentation-agnostic by design (mirrors <see cref="RowEditPanel"/>):
    /// <see cref="LookupDialog"/> hosts it in a modal window, but the panel can be
    /// embedded anywhere and unit-tested without a window.
    /// </remarks>
    public class LookupPanel : UserControl
    {
        private readonly TextBox _searchBox;
        private readonly GridControl _grid;
        private readonly Button _okButton;
        private readonly TextBlock _errorLabel;
        private FormApiConnector? _connector;

        /// <summary>
        /// Raised when a row is committed (OK button or row double-click).
        /// </summary>
        public event EventHandler<DataRow>? Committed;

        /// <summary>
        /// Raised when the selection is cancelled.
        /// </summary>
        public event EventHandler? Cancelled;

        /// <summary>
        /// Initializes a new instance of <see cref="LookupPanel"/>.
        /// </summary>
        public LookupPanel()
        {
            _searchBox = new TextBox { PlaceholderText = "Search", MinWidth = 200 };
            _searchBox.KeyDown += async (_, e) =>
            {
                if (e.Key != Key.Enter) return;
                e.Handled = true;
                await ReloadAsync().ConfigureAwait(true);
            };
            var searchButton = new Button { Content = "Search" };
            searchButton.Click += async (_, _) => await ReloadAsync().ConfigureAwait(true);

            _grid = new GridControl { MinHeight = 240 };
            _grid.RowSelected += (_, _) => UpdateOkState();
            _grid.InnerGrid.DoubleTapped += (_, _) => Commit();

            _okButton = new Button { Content = "OK", MinWidth = 80, IsEnabled = false };
            _okButton.Click += (_, _) => Commit();
            var cancelButton = new Button { Content = "Cancel", MinWidth = 80 };
            cancelButton.Click += (_, _) => Cancel();

            _errorLabel = new TextBlock
            {
                IsVisible = false,
                Foreground = Brushes.IndianRed,
                TextWrapping = TextWrapping.Wrap,
            };

            var searchRow = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(searchButton, Dock.Right);
            searchButton.Margin = new Thickness(8, 0, 0, 0);
            searchRow.Children.Add(searchButton);
            searchRow.Children.Add(_searchBox);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            buttons.Children.Add(_okButton);
            buttons.Children.Add(cancelButton);

            var host = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 12,
                Margin = new Thickness(16),
                MinWidth = 420,
            };
            host.Children.Add(searchRow);
            host.Children.Add(_errorLabel);
            host.Children.Add(_grid);
            host.Children.Add(buttons);
            Content = host;
        }

        /// <summary>
        /// Gets the inner grid for host-level tweaks and tests.
        /// </summary>
        public GridControl Grid => _grid;

        /// <summary>
        /// Gets the currently selected lookup row, or <c>null</c>.
        /// </summary>
        public DataRow? SelectedRow => (_grid.SelectedItem as DataRowView)?.Row;

        /// <summary>
        /// Gets or sets the search text submitted to the server on reload.
        /// </summary>
        public string SearchText
        {
            get => _searchBox.Text ?? string.Empty;
            set => _searchBox.Text = value;
        }

        /// <summary>
        /// Binds the panel to the lookup source: columns come from the schema's
        /// lookup layout, rows from <paramref name="connector"/> on
        /// <see cref="ReloadAsync"/>.
        /// </summary>
        /// <param name="schema">The target form's schema.</param>
        /// <param name="connector">The connector for the target form.</param>
        public void Bind(FormSchema schema, FormApiConnector connector)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(connector);

            _connector = connector;
            _grid.Bind(schema.GetLookupLayout(), rows: null);
            UpdateOkState();
        }

        /// <summary>
        /// Reloads the lookup rows from the server using the current
        /// <see cref="SearchText"/>.
        /// </summary>
        public async Task ReloadAsync()
        {
            var connector = _connector;
            if (connector is null) return;

            try
            {
                ClearError();
                var response = await connector.GetLookupAsync(SearchText).ConfigureAwait(true);
                _grid.DataTable = response.Table;
                UpdateOkState();
            }
            catch (Exception ex)
            {
                // UI boundary: surface the failure on the panel instead of crashing
                // the picker (mirrors FormView's ReportError behaviour).
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// Commits the current selection (equivalent to pressing OK); a commit
        /// without a selection is a no-op.
        /// </summary>
        public void Commit()
        {
            var row = SelectedRow;
            if (row is null) return;
            Committed?.Invoke(this, row);
        }

        /// <summary>
        /// Cancels the selection (equivalent to pressing Cancel).
        /// </summary>
        public void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateOkState()
        {
            _okButton.IsEnabled = SelectedRow is not null;
        }

        private void ShowError(string message)
        {
            _errorLabel.Text = message;
            _errorLabel.IsVisible = true;
        }

        private void ClearError()
        {
            _errorLabel.Text = string.Empty;
            _errorLabel.IsVisible = false;
        }
    }
}
