using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;

namespace Avalonia.DemoCenter.Modules.DataEditors
{
    /// <summary>
    /// <see cref="CheckEdit"/> scenario: a boolean checkbox bound to a field with a live
    /// value readout, plus a per-field read-only variant.
    /// </summary>
    public sealed class CheckEditModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Data Editors";

        /// <inheritdoc/>
        public override string ControlName => "CheckEdit";

        /// <inheritdoc/>
        public override string ScenarioTitle => "綁定 · 唯讀";

        /// <inheritdoc/>
        public override string Description =>
            "布林勾選編輯器。示範 FormScope ambient 綁定與 LayoutField.ReadOnly 唯讀外觀。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = DataEditorParts.SingleField(
                "active", "Active", FieldDbType.Boolean,
                initialValue: bool.TrueString);

            var bound = new CheckEdit { FieldName = "active", Content = "Active" };

            var readOnly = new CheckEdit { Content = "Active" };
            readOnly.Bind(data, new LayoutField { FieldName = "active", ReadOnly = true });

            return DataEditorParts.Compose(
                data,
                DataEditorParts.Section(
                    "基本綁定",
                    "勾選 / 取消後下方值即時更新（True / False）。工具列 FormMode 切 View 時轉唯讀。",
                    DataEditorParts.LabeledRow("active", bound),
                    DataEditorParts.LiveValue(data, "active")),
                DataEditorParts.Section(
                    "唯讀（LayoutField.ReadOnly）",
                    "此欄以 ReadOnly=true 綁定，無論 FormMode 為何都唯讀（停用，不可勾選）。",
                    DataEditorParts.LabeledRow("active", readOnly)));
        }
    }
}
