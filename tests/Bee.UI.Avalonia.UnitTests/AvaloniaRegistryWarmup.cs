using System.Data;
using System.Runtime.CompilerServices;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;

namespace Bee.UI.Avalonia.UnitTests
{
    /// <summary>
    /// Avalonia's <c>AvaloniaPropertyRegistry</c> caches the per-type direct-property
    /// lists in plain dictionaries whose first population is not thread-safe. xUnit
    /// runs test classes in parallel and many of them construct the same controls,
    /// which intermittently raced with
    /// "An item with the same key has already been added" inside
    /// <c>GetRegisteredDirect</c>. Warming the registry once here — module
    /// initializers run single-threaded before any test — removes the race.
    /// </summary>
    internal static class AvaloniaRegistryWarmup
    {
        [ModuleInitializer]
        internal static void Warm()
        {
            // Setting a direct property routes through GetRegisteredDirect and
            // populates the registry cache for the concrete type.
            new GridControl().DataTable = new DataTable("warmup");
            new DropDownEdit().SelectedItem = null;

            // `ComboBox.UpdateSelectionBoxItem` parses an indexer binding for the
            // selection box the first time a selected item renders; that parse
            // populates a shared expression-AST cache whose first population is not
            // thread-safe either (observed as "Collection was modified" inside
            // `ExpressionNodeFactory.CreateFromAst` on 2-core CI runners). Selecting
            // an item here walks that path once, single-threaded.
            var combo = new DropDownEdit
            {
                ItemsSource = new[] { new Bee.Definition.Collections.ListItem("warm", "Warm") },
            };
            combo.SelectedIndex = 0;

            // Constructing the remaining controls warms their styled-property paths.
            _ = new TextEdit();
            _ = new MemoEdit();
            _ = new ButtonEdit();
            _ = new DateEdit();
            _ = new YearMonthEdit();
            _ = new CheckEdit();
            _ = new DynamicForm();
            _ = new RowEditPanel();
        }
    }
}
