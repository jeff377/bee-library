using Avalonia.Controls;
using Avalonia.Layout;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;
using Avalonia.DemoCenter.Modules.DataEditors;
using Avalonia.DemoCenter.Modules.Views;

namespace Avalonia.DemoCenter.Modules.FormModes
{
    /// <summary>
    /// Interactive FormMode switch: a View / Add / Edit selector drives a set of ambient-bound
    /// controls and a detail grid live, via <see cref="FormScope.SetFormMode"/> on their
    /// container. This is where FormMode switching lives — the shell toolbar has none, so it
    /// never affects unrelated demos.
    /// </summary>
    public sealed class InteractiveFormModeModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "FormMode 顯示狀態";

        /// <inheritdoc/>
        public override string Title => "互動切換";

        /// <inheritdoc/>
        public override string Description =>
            "用下方 FormMode 下拉切 View / Add / Edit，即時驅動這組控件與明細 grid 的唯讀 / 編輯狀態"
            + "（View：去框唯讀、ButtonEdit 圖示隱藏、grid 唯讀且工具列隱藏）。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = SampleFormData.BuildMasterDetail(SampleFormData.BuildSchema());

            var grid = new GridControl { MinHeight = 140 };
            grid.Bind(data, SampleFormData.BuildPhonesLayout());

            // Controls container: owns the ambient data object and the FormMode the combo drives.
            var controls = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    DataEditorParts.Section(
                        "主檔控件",
                        null,
                        DataEditorParts.LabeledRow("emp_name (TextEdit)", new TextEdit { FieldName = "emp_name" }),
                        DataEditorParts.LabeledRow("dept (DropDownEdit)", new DropDownEdit { FieldName = "dept" }),
                        DataEditorParts.LabeledRow("hire_date (DateEdit)", new DateEdit { FieldName = "hire_date" }),
                        DataEditorParts.LabeledRow("is_active (CheckEdit)", new CheckEdit { FieldName = "is_active", Content = "Active" }),
                        DataEditorParts.LabeledRow("emp_code (ButtonEdit)", new ButtonEdit { FieldName = "emp_code" })),
                    DataEditorParts.Section("Phones 明細 (GridControl)", null, grid),
                },
            };
            FormScope.SetDataObject(controls, data);
            FormScope.SetFormMode(controls, SingleFormMode.Edit);

            var combo = new ComboBox
            {
                ItemsSource = Enum.GetValues<SingleFormMode>(),
                SelectedItem = SingleFormMode.Edit,
                MinWidth = 120,
            };
            combo.SelectionChanged += (_, _) =>
            {
                if (combo.SelectedItem is SingleFormMode mode)
                    FormScope.SetFormMode(controls, mode);
            };

            var bar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "FormMode", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.8 },
                    combo,
                },
            };

            var root = new StackPanel { Spacing = 12, Margin = new Thickness(4), Children = { bar, controls } };
            return new ScrollViewer { Content = root };
        }
    }
}
