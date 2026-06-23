using System.Data;
using Avalonia;
using Avalonia.Controls;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Modal presentation of <see cref="RowEditPanel"/> for the
    /// <see cref="GridEditMode.EditForm"/> mode. A popup keeps the layout stable
    /// regardless of how many rows the grid holds — the inline-panel alternative
    /// shifts the layout and sits far from the selected row on long grids.
    /// </summary>
    public static class RowEditDialog
    {
        /// <summary>
        /// Opens the edit form for <paramref name="row"/> and returns whether the
        /// user committed the edit. Closing the window without choosing rolls the
        /// session back (the panel cancels on detach).
        /// </summary>
        /// <param name="host">A visual inside the owning window; used to resolve the dialog owner.</param>
        /// <param name="dataObject">The data object that owns the row.</param>
        /// <param name="layout">The grid layout whose columns drive the editors.</param>
        /// <param name="row">The row to edit.</param>
        public static async Task<bool> ShowAsync(Visual host, FormDataObject dataObject, LayoutGrid layout, DataRow row)
        {
            ArgumentNullException.ThrowIfNull(host);
            ArgumentNullException.ThrowIfNull(dataObject);
            ArgumentNullException.ThrowIfNull(layout);
            ArgumentNullException.ThrowIfNull(row);

            var panel = new RowEditPanel();
            var committed = false;
            var title = string.IsNullOrEmpty(layout.Caption) ? layout.TableName : layout.Caption;
            panel.Bind(dataObject, layout, row);

            // Browser (WASM) hosts cannot open a native Window; host the panel on the top
            // level's OverlayLayer instead. RowEditPanel cancels its buffered edit when it
            // detaches from the tree, so removing the overlay rolls back an uncommitted edit
            // exactly like closing the window did. Desktop keeps the native modal window.
            if (OperatingSystem.IsBrowser())
            {
                var completed = new TaskCompletionSource();
                panel.EditCommitted += (_, _) => { committed = true; completed.TrySetResult(); };
                panel.EditCancelled += (_, _) => completed.TrySetResult();
                await OverlayDialogHost.ShowAsync(host, panel, title, completed.Task);
                return committed;
            }

            var window = new Window
            {
                Title = title,
                Content = panel,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false,
            };
            panel.EditCommitted += (_, _) => { committed = true; window.Close(); };
            panel.EditCancelled += (_, _) => window.Close();

            if (TopLevel.GetTopLevel(host) is Window owner)
            {
                await window.ShowDialog(owner);
            }
            else
            {
                // Embedded hosts without a Window cannot parent a modal dialog;
                // fall back to a free-standing window and await its closure.
                var closed = new TaskCompletionSource();
                window.Closed += (_, _) => closed.TrySetResult();
                window.Show();
                await closed.Task;
            }

            return committed;
        }
    }
}
