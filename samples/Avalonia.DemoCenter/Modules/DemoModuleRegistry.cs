using Avalonia.DemoCenter.Modules.DataEditors;
using Avalonia.DemoCenter.Modules.Grids;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules
{
    /// <summary>
    /// Central registry of every demo module. The navigation tree is generated from this
    /// list (grouped by <see cref="IDemoModule.Category"/> then
    /// <see cref="IDemoModule.ControlName"/>), so registering a new module here is all it
    /// takes to surface it in the shell.
    /// </summary>
    public static class DemoModuleRegistry
    {
        /// <summary>All registered demo modules, in navigation order.</summary>
        public static IReadOnlyList<IDemoModule> Modules { get; } =
        [
            // Per-editor scenarios.
            new TextEditModule(),
            new MemoEditModule(),
            new ButtonEditModule(),
            new DateEditModule(),
            new YearMonthEditModule(),
            new DropDownEditModule(),
            new CheckEditModule(),
            // Grid scenarios.
            new GridControlModule(),
            // View scenarios (FormView / ListView patterns, backend-free).
            new FormViewModule(),
            new ListViewModule(),
            // Overview: all editors, native vs inherited (the migrated gallery regression).
            new EditorsComparisonModule(),
        ];
    }
}
