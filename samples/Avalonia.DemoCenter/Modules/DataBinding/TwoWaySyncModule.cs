using Avalonia.Controls;
using Bee.Base.Data;
using Bee.UI.Avalonia.Controls.Editors;
using Avalonia.DemoCenter.Modules.DataEditors;

namespace Avalonia.DemoCenter.Modules.DataBinding
{
    /// <summary>
    /// Two-way sync: two editors bound to the same field. Editing either one writes through
    /// the data object and the other refreshes via <c>FieldValueChanged</c> — the data
    /// object is the single source of truth.
    /// </summary>
    public sealed class TwoWaySyncModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "資料繫結";

        /// <inheritdoc/>
        public override string Title => "即時雙向同步";

        /// <inheritdoc/>
        public override string Description =>
            "兩個控件綁同一欄位：在任一個輸入，另一個與下方即時值同步更新（FormDataObject 為單一真實來源）。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = DataEditorParts.SingleField("name", "Name", FieldDbType.String, initialValue: "Alice Chen");

            return DataEditorParts.Compose(
                data,
                DataEditorParts.Section(
                    "兩控件綁同一欄位 name",
                    "在上面任一個輸入框打字，另一個立即同步。",
                    DataEditorParts.LabeledRow("編輯器 A", new TextEdit { FieldName = "name" }),
                    DataEditorParts.LabeledRow("編輯器 B", new TextEdit { FieldName = "name" })),
                DataEditorParts.Section(
                    "即時值",
                    null,
                    DataEditorParts.LiveValue(data, "name")));
        }
    }
}
