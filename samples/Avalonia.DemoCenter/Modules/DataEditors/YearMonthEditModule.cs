using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;

namespace Avalonia.DemoCenter.Modules.DataEditors
{
    /// <summary>
    /// <see cref="YearMonthEdit"/> scenario: year-month binding (no day component) with a
    /// live value readout, plus a per-field read-only variant.
    /// </summary>
    public sealed class YearMonthEditModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Data Editors";

        /// <inheritdoc/>
        public override string ControlName => "YearMonthEdit";

        /// <inheritdoc/>
        public override string ScenarioTitle => "綁定 · 唯讀";

        /// <inheritdoc/>
        public override string Description =>
            "年月編輯器（DayVisible=False，值為 yyyy-MM 字串）。示範 FormScope ambient 綁定與 LayoutField.ReadOnly 唯讀外觀。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = DataEditorParts.SingleField(
                "pay_month", "Pay Month", FieldDbType.String,
                initialValue: "2026-06");

            var bound = new YearMonthEdit { FieldName = "pay_month" };

            var readOnly = new YearMonthEdit();
            readOnly.Bind(data, new LayoutField { FieldName = "pay_month", ReadOnly = true });

            return DataEditorParts.Compose(
                data,
                DataEditorParts.Section(
                    "基本綁定（年月）",
                    "選年月後下方值即時更新（yyyy-MM）。工具列 FormMode 切 View 時整欄轉唯讀。",
                    DataEditorParts.LabeledRow("pay_month", bound),
                    DataEditorParts.LiveValue(data, "pay_month")),
                DataEditorParts.Section(
                    "唯讀（LayoutField.ReadOnly）",
                    "此欄以 ReadOnly=true 綁定，無論 FormMode 為何都唯讀。",
                    DataEditorParts.LabeledRow("pay_month", readOnly)));
        }
    }
}
