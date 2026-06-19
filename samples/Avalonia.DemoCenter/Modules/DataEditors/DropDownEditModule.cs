using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;

namespace Avalonia.DemoCenter.Modules.DataEditors
{
    /// <summary>
    /// <see cref="DropDownEdit"/> scenario: a combo bound to a field whose options come
    /// from <c>FormField.ListItems</c>, with a live value readout and a read-only variant.
    /// </summary>
    public sealed class DropDownEditModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Data Editors";

        /// <inheritdoc/>
        public override string ControlName => "DropDownEdit";

        /// <inheritdoc/>
        public override string ScenarioTitle => "綁定 · ListItems · 唯讀";

        /// <inheritdoc/>
        public override string Description =>
            "下拉編輯器。選項來自 FormField.ListItems（顯示文字 vs 寫回值）。示範 ambient 綁定與 LayoutField.ReadOnly。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = DataEditorParts.SingleField(
                "dept", "Department", FieldDbType.String,
                configure: f =>
                {
                    f.ListItems!.Add("HR", "Human Resources");
                    f.ListItems.Add("IT", "Information Technology");
                    f.ListItems.Add("FIN", "Finance");
                },
                initialValue: "IT");

            var bound = new DropDownEdit { FieldName = "dept" };

            var readOnly = new DropDownEdit();
            readOnly.Bind(data, new LayoutField { FieldName = "dept", ReadOnly = true });

            return DataEditorParts.Compose(
                data,
                DataEditorParts.Section(
                    "基本綁定 + ListItems",
                    "下拉顯示 ListItems 的文字（Human Resources…），寫回的是值（HR / IT / FIN）——見下方即時值。",
                    DataEditorParts.LabeledRow("dept", bound),
                    DataEditorParts.LiveValue(data, "dept")),
                DataEditorParts.Section(
                    "唯讀（LayoutField.ReadOnly）",
                    "此欄以 ReadOnly=true 綁定，唯讀時顯示選定項的文字、不可展開。",
                    DataEditorParts.LabeledRow("dept", readOnly)));
        }
    }
}
