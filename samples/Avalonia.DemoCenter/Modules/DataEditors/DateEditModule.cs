using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;

namespace Avalonia.DemoCenter.Modules.DataEditors
{
    /// <summary>
    /// <see cref="DateEdit"/> scenario: date binding with a live value readout, plus a
    /// per-field read-only variant.
    /// </summary>
    public sealed class DateEditModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Data Editors";

        /// <inheritdoc/>
        public override string ControlName => "DateEdit";

        /// <inheritdoc/>
        public override string ScenarioTitle => "綁定 · 唯讀";

        /// <inheritdoc/>
        public override string Description =>
            "日期編輯器（三段式日期選輪）。示範 FormScope ambient 綁定與 LayoutField.ReadOnly 唯讀外觀。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = DataEditorParts.SingleField(
                "hire_date", "Hire Date", FieldDbType.Date,
                initialValue: "2026-06-11");

            var bound = new DateEdit { FieldName = "hire_date" };

            var readOnly = new DateEdit();
            readOnly.Bind(data, new LayoutField { FieldName = "hire_date", ReadOnly = true });

            return DataEditorParts.Compose(
                data,
                DataEditorParts.Section(
                    "基本綁定",
                    "選日期後下方值即時更新（ISO 格式）。工具列 FormMode 切 View 時整欄轉唯讀。",
                    DataEditorParts.LabeledRow("hire_date", bound),
                    DataEditorParts.LiveValue(data, "hire_date")),
                DataEditorParts.Section(
                    "唯讀（LayoutField.ReadOnly）",
                    "此欄以 ReadOnly=true 綁定，無論 FormMode 為何都唯讀。",
                    DataEditorParts.LabeledRow("hire_date", readOnly)));
        }
    }
}
