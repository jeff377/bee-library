using System.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// The editing surface of the <see cref="GridEditMode.EditForm"/> mode: renders
    /// one field editor per visible <see cref="LayoutColumn"/>, bound to a single
    /// row through the buffered edit protocol of <see cref="FormDataObject"/> —
    /// OK commits (<see cref="FormDataObject.CommitRowEdit"/>), Cancel restores
    /// (<see cref="FormDataObject.CancelRowEdit"/>).
    /// </summary>
    /// <remarks>
    /// Presentation-agnostic by design: <see cref="RowEditDialog"/> hosts it in a
    /// modal window, but the panel itself can be embedded anywhere (and unit-tested
    /// without a window). Detaching from the logical tree cancels an in-progress
    /// edit, so closing a hosting window never leaks a half-committed session.
    /// </remarks>
    public class RowEditPanel : UserControl
    {
        private const int ColumnCount = 2;

        private readonly List<IFieldEditor> _editors = [];
        private FormDataObject? _dataObject;
        private DataRow? _row;

        /// <summary>
        /// Raised after the OK button commits the edit session.
        /// </summary>
        public event EventHandler? EditCommitted;

        /// <summary>
        /// Raised after the Cancel button rolls the edit session back.
        /// </summary>
        public event EventHandler? EditCancelled;

        /// <summary>
        /// Gets the row currently being edited, or <c>null</c>.
        /// </summary>
        public DataRow? Row => _row;

        /// <summary>
        /// Starts a buffered edit session on <paramref name="row"/> and builds the
        /// editor surface from <paramref name="layout"/>. A previous unbound session
        /// is cancelled first.
        /// </summary>
        /// <param name="dataObject">The data object that owns the row.</param>
        /// <param name="layout">The grid layout whose columns drive the editors.</param>
        /// <param name="row">The row to edit.</param>
        public void Bind(FormDataObject dataObject, LayoutGrid layout, DataRow row)
        {
            ArgumentNullException.ThrowIfNull(dataObject);
            ArgumentNullException.ThrowIfNull(layout);
            ArgumentNullException.ThrowIfNull(row);

            Unbind();
            _dataObject = dataObject;
            _row = row;
            dataObject.BeginRowEdit(row);
            Content = BuildContent(dataObject, layout, row);
        }

        /// <summary>
        /// Commits the edit session (equivalent to pressing OK).
        /// </summary>
        public void Commit()
        {
            if (_dataObject is null || _row is null) return;
            var dataObject = _dataObject;
            var row = _row;

            ReleaseEditors();
            Reset();
            dataObject.CommitRowEdit(row);
            EditCommitted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Cancels the edit session (equivalent to pressing Cancel).
        /// </summary>
        public void Cancel()
        {
            if (_dataObject is null || _row is null) return;
            var dataObject = _dataObject;
            var row = _row;

            ReleaseEditors();
            Reset();
            dataObject.CancelRowEdit(row);
            EditCancelled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Releases the binding; an in-progress edit session is cancelled without
        /// raising <see cref="EditCancelled"/> (the host initiated the teardown).
        /// </summary>
        public void Unbind()
        {
            if (_dataObject is not null && _row is not null)
            {
                var dataObject = _dataObject;
                var row = _row;
                ReleaseEditors();
                Reset();
                dataObject.CancelRowEdit(row);
                return;
            }
            ReleaseEditors();
            Reset();
        }

        /// <inheritdoc />
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);
            // A hosting window that closes without OK/Cancel must not leave the row
            // in edit state.
            Unbind();
        }

        private StackPanel BuildContent(FormDataObject dataObject, LayoutGrid layout, DataRow row)
        {
            var grid = new Grid
            {
                ColumnSpacing = 12,
                RowSpacing = 8,
            };
            for (var i = 0; i < ColumnCount; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var index = 0;
            foreach (var column in EnumerateVisibleColumns(layout))
            {
                var cell = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2 };
                cell.Children.Add(new TextBlock { Text = column.Caption });

                var editor = FieldEditorFactory.Create(column.ControlType);
                ((IFieldEditor)editor).Bind(dataObject, column, row);
                _editors.Add((IFieldEditor)editor);
                cell.Children.Add(editor);

                var rowIndex = index / ColumnCount;
                while (grid.RowDefinitions.Count <= rowIndex)
                    grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                Grid.SetRow(cell, rowIndex);
                Grid.SetColumn(cell, index % ColumnCount);
                grid.Children.Add(cell);
                index++;
            }

            var okButton = new Button { Content = "OK", MinWidth = 80 };
            okButton.Click += (_, _) => Commit();
            var cancelButton = new Button { Content = "Cancel", MinWidth = 80 };
            cancelButton.Click += (_, _) => Cancel();

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            buttons.Children.Add(okButton);
            buttons.Children.Add(cancelButton);

            var host = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 12,
                Margin = new Thickness(16),
                MinWidth = 360,
            };
            host.Children.Add(grid);
            host.Children.Add(buttons);
            return host;
        }

        private void ReleaseEditors()
        {
            foreach (var editor in _editors)
                editor.Unbind();
            _editors.Clear();
        }

        private void Reset()
        {
            _dataObject = null;
            _row = null;
            Content = null;
        }

        private static IEnumerable<LayoutColumn> EnumerateVisibleColumns(LayoutGrid layout)
            => layout.Columns?.Where(c => c.Visible) ?? Enumerable.Empty<LayoutColumn>();
    }
}
