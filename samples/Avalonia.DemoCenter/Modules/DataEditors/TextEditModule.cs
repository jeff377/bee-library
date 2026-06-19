using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;

namespace Avalonia.DemoCenter.Modules.DataEditors
{
    /// <summary>
    /// <see cref="TextEdit"/> scenario: ambient field binding with a live value readout
    /// and metadata (<c>MaxLength</c>), plus a per-field read-only variant.
    /// </summary>
    public sealed class TextEditModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "Data Editors";

        /// <inheritdoc/>
        public override string ControlName => "TextEdit";

        /// <inheritdoc/>
        public override string ScenarioTitle => "綁定 · Metadata · 唯讀";

        /// <inheritdoc/>
        public override string Description =>
            "單行文字編輯器。示範 FormScope ambient 綁定、MaxLength metadata，以及 LayoutField.ReadOnly 唯讀外觀。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = DataEditorParts.SingleField(
                "name", "Name", FieldDbType.String,
                configure: f => f.MaxLength = 10,
                initialValue: "Alice");

            var bound = new TextEdit { FieldName = "name" };

            var readOnly = new TextEdit();
            readOnly.Bind(data, new LayoutField { FieldName = "name", ReadOnly = true });

            return DataEditorParts.Compose(
                data,
                DataEditorParts.Section(
                    "基本綁定 + Metadata（MaxLength=10）",
                    "在輸入框打字，下方值即時更新；超過 10 字無法輸入。工具列 FormMode 切 View 時整欄轉唯讀。",
                    DataEditorParts.LabeledRow("name", bound),
                    DataEditorParts.LiveValue(data, "name")),
                DataEditorParts.Section(
                    "唯讀（LayoutField.ReadOnly）",
                    "此欄以 ReadOnly=true 綁定，無論 FormMode 為何都唯讀——去框、僅顯示值。",
                    DataEditorParts.LabeledRow("name", readOnly)));
        }
    }
}
