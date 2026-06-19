using Avalonia.Controls;
using Avalonia.Layout;
using Bee.Base.Data;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Avalonia.DemoCenter.Modules.DataEditors
{
    /// <summary>
    /// <see cref="ButtonEdit"/> scenario: a text editor with a trailing icon button.
    /// Without relation metadata the icon raises <see cref="ButtonEdit.ButtonClick"/>;
    /// this module wires it to a small local picker that writes the choice back.
    /// </summary>
    /// <remarks>
    /// The production lookup flow (<c>FormField.RelationProgId</c> → <c>LookupDialog</c>
    /// resolving against the backend) is shown end-to-end in <c>Avalonia.Demo</c>; this
    /// demo center has no backend, so it demonstrates the <see cref="ButtonEdit.ButtonClick"/>
    /// picker path instead.
    /// </remarks>
    public sealed class ButtonEditModule : DemoModuleBase
    {
        private static readonly string[] s_codes = ["EMP-001", "EMP-002", "EMP-003", "EMP-004"];

        /// <inheritdoc/>
        public override string Category => "Data Editors";

        /// <inheritdoc/>
        public override string ControlName => "ButtonEdit";

        /// <inheritdoc/>
        public override string ScenarioTitle => "綁定 · Picker · 唯讀";

        /// <inheritdoc/>
        public override string Description =>
            "帶內嵌圖示按鈕的文字編輯器。本範例無後端，以 ButtonClick 開本機 picker 寫回值；"
            + "生產環境的 RelationProgId → LookupDialog 後端查詢流程見 Avalonia.Demo。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = DataEditorParts.SingleField(
                "code", "Code", FieldDbType.String,
                initialValue: "EMP-001");

            var bound = new ButtonEdit { FieldName = "code" };
            bound.ButtonClick += (_, _) => _ = ShowPickerAsync(bound, data);

            var readOnly = new ButtonEdit();
            readOnly.Bind(data, new LayoutField { FieldName = "code", ReadOnly = true });

            return DataEditorParts.Compose(
                data,
                DataEditorParts.Section(
                    "基本綁定 + 本機 Picker",
                    "點右側放大鏡圖示開啟 picker，選取後值即時寫回。工具列 FormMode 切 View 時圖示隱藏、整欄轉唯讀。",
                    DataEditorParts.LabeledRow("code", bound),
                    DataEditorParts.LiveValue(data, "code")),
                DataEditorParts.Section(
                    "唯讀（LayoutField.ReadOnly）",
                    "此欄以 ReadOnly=true 綁定，唯讀時圖示隱藏、僅顯示值。",
                    DataEditorParts.LabeledRow("code", readOnly)));
        }

        // A minimal in-app picker standing in for the backend lookup dialog: pick a code
        // from a fixed list and write it back through the data object.
        private static async Task ShowPickerAsync(Control anchor, FormDataObject data)
        {
            if (TopLevel.GetTopLevel(anchor) is not Window owner)
                return;

            var list = new ListBox
            {
                ItemsSource = s_codes,
                SelectedItem = data.GetField("code"),
                Margin = new Thickness(8),
            };

            string? result = null;
            var ok = new Button { Content = "選取", IsDefault = true, MinWidth = 72 };
            var cancel = new Button { Content = "取消", IsCancel = true, MinWidth = 72 };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Margin = new Thickness(8),
                Children = { ok, cancel },
            };
            DockPanel.SetDock(buttons, Dock.Bottom);

            var dialog = new Window
            {
                Title = "選擇代碼",
                Width = 260,
                Height = 320,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new DockPanel { Children = { buttons, list } },
            };

            ok.Click += (_, _) => { result = list.SelectedItem as string; dialog.Close(); };
            cancel.Click += (_, _) => dialog.Close();

            await dialog.ShowDialog(owner);

            if (result is not null)
                data.SetField("code", result);
        }
    }
}
