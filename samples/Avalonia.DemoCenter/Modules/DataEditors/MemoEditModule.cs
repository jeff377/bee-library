using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;

namespace Avalonia.DemoCenter.Modules.DataEditors
{
    /// <summary>
    /// <see cref="MemoEdit"/> scenario: multi-line text binding with a live value readout,
    /// plus a per-field read-only variant.
    /// </summary>
    public sealed class MemoEditModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Data Editors";

        /// <inheritdoc/>
        public override string ControlName => "MemoEdit";

        /// <inheritdoc/>
        public override string ScenarioTitle => "綁定 · 多行 · 唯讀";

        /// <inheritdoc/>
        public override string Description =>
            "多行文字編輯器（AcceptsReturn + 自動換行）。示範 FormScope ambient 綁定與 LayoutField.ReadOnly 唯讀外觀。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = DataEditorParts.SingleField(
                "memo", "Notes", FieldDbType.String,
                initialValue: "第一行" + "\n" + "第二行");

            var bound = new MemoEdit { FieldName = "memo", MinHeight = 80 };

            var readOnly = new MemoEdit { MinHeight = 80 };
            readOnly.Bind(data, new LayoutField { FieldName = "memo", ReadOnly = true });

            return DataEditorParts.Compose(
                data,
                DataEditorParts.Section(
                    "基本綁定（多行）",
                    "Enter 換行；下方值即時更新（含換行字元）。工具列 FormMode 切 View 時整欄轉唯讀。",
                    DataEditorParts.LabeledRow("memo", bound),
                    DataEditorParts.LiveValue(data, "memo")),
                DataEditorParts.Section(
                    "唯讀（LayoutField.ReadOnly）",
                    "此欄以 ReadOnly=true 綁定，無論 FormMode 為何都唯讀。",
                    DataEditorParts.LabeledRow("memo", readOnly)));
        }
    }
}
