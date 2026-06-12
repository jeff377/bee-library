using System.Data;
using Avalonia;
using Avalonia.Controls;
using Bee.Api.Client.Connectors;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.UI.Core;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Modal presentation of <see cref="LookupPanel"/>: opens the lookup picker for
    /// a target program and returns the selected row, or <c>null</c> when the user
    /// cancels. Schema and connector default to the ambient <see cref="ClientInfo"/>
    /// wiring; pass them explicitly to bypass it (tests, custom hosts).
    /// </summary>
    public static class LookupDialog
    {
        /// <summary>
        /// Opens the lookup picker for <paramref name="progId"/> and returns the
        /// selected row, or <c>null</c> when the user cancels.
        /// </summary>
        /// <param name="host">A visual inside the owning window; used to resolve the dialog owner.</param>
        /// <param name="progId">The target program identifier (the lookup source form).</param>
        /// <param name="schema">The target form's schema; <c>null</c> loads it through <see cref="ClientInfo.SystemApiConnector"/>.</param>
        /// <param name="connector">The connector for the target form; <c>null</c> creates one through <see cref="ClientInfo.CreateFormApiConnector"/>.</param>
        public static async Task<DataRow?> ShowAsync(
            Visual host,
            string progId,
            FormSchema? schema = null,
            FormApiConnector? connector = null)
        {
            ArgumentNullException.ThrowIfNull(host);
            ArgumentException.ThrowIfNullOrWhiteSpace(progId);

            schema ??= await ClientInfo.SystemApiConnector
                .GetDefineAsync<FormSchema>(DefineType.FormSchema, [progId])
                .ConfigureAwait(true);
            if (schema is null)
                throw new InvalidOperationException($"FormSchema '{progId}' was not found for lookup.");
            connector ??= ClientInfo.CreateFormApiConnector(progId);

            var panel = new LookupPanel();
            var window = new Window
            {
                Title = schema.DisplayName,
                Content = panel,
                Width = 520,
                Height = 480,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false,
            };

            DataRow? selected = null;
            panel.Committed += (_, row) => { selected = row; window.Close(); };
            panel.Cancelled += (_, _) => window.Close();
            panel.Bind(schema, connector);
            // Fire-and-forget: load failures surface on the panel's error label,
            // and the dialog stays usable (retry via the search button).
            _ = panel.ReloadAsync();

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

            return selected;
        }
    }
}
