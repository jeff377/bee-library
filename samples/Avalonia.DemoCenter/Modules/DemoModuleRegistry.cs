using Avalonia.DemoCenter.Modules.ControlTypes;
using Avalonia.DemoCenter.Modules.DataBinding;
using Avalonia.DemoCenter.Modules.FormModes;
using Avalonia.DemoCenter.Modules.Grids;
using Avalonia.DemoCenter.Modules.Layouts;
using Avalonia.DemoCenter.Modules.Lookup;
using Avalonia.DemoCenter.Modules.MasterDetail;
using Avalonia.DemoCenter.Modules.ReadOnlyRequired;

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
            // 資料繫結 (Data Binding).
            new AmbientBindingModule(),
            new ExplicitBindingModule(),
            new TwoWaySyncModule(),
            new DataObjectEventsModule(),
            // 唯讀與必填 (Read-only & Required).
            new ReadOnlyFieldModule(),
            new RequiredReadOnlyMarkModule(),
            // FormMode 顯示狀態.
            new InteractiveFormModeModule(),
            new FormModeStatesModule(),
            new GridFormModeModule(),
            // 開窗選資料 (Lookup).
            new LookupPickerModule(),
            // Layout 排版.
            new AutoFormLayoutModule(),
            new MultiColumnLayoutModule(),
            // Grid.
            new InCellEditModule(),
            new EditFormModule(),
            new AmbientGridModule(),
            new ListModeModule(),
            new NumberFormatModule(),
            // Master-Detail.
            new MasterDetailModule(),
        ];
    }
}
