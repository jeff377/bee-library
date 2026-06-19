using Avalonia.DemoCenter.Modules.ControlTypes;

namespace Avalonia.DemoCenter.Modules
{
    /// <summary>
    /// Central registry of every demo module. The navigation tree is generated from this
    /// list, grouped by <see cref="IDemoModule.Category"/> (theme) into a two-level tree
    /// (theme → case), so registering a new module here is all it takes to surface it.
    /// </summary>
    public static class DemoModuleRegistry
    {
        /// <summary>All registered demo modules, in navigation order.</summary>
        public static IReadOnlyList<IDemoModule> Modules { get; } =
        [
            // 控件類型 (ControlTypes).
            new ControlGalleryModule(),
            new FieldControlComparisonModule(),
            new TableControlComparisonModule(),
        ];
    }
}
