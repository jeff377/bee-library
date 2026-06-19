using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;
using Avalonia.DemoCenter.Modules.DataEditors;

namespace Avalonia.DemoCenter.Modules.DataBinding
{
    /// <summary>
    /// Ambient binding: the container sets the <see cref="FormScope"/> data object once and
    /// every descendant editor with a <c>FieldName</c> binds itself on attach.
    /// </summary>
    public sealed class AmbientBindingModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "資料繫結";

        /// <inheritdoc/>
        public override string Title => "Ambient 繫結";

        /// <inheritdoc/>
        public override string Description =>
            "容器以 FormScope.SetDataObject 設一次 DataObject；子編輯器只給 FieldName，attach 時自動綁定，免逐一接線。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = BuildData();
            return DataEditorParts.Compose(
                data,
                DataEditorParts.Section(
                    "容器設一次，子控件自動綁定",
                    "本視圖根節點已 FormScope.SetDataObject(data)；下列編輯器只設 FieldName。",
                    DataEditorParts.LabeledRow("name (TextEdit)", new TextEdit { FieldName = "name" }),
                    DataEditorParts.LabeledRow("dept (DropDownEdit)", new DropDownEdit { FieldName = "dept" })),
                DataEditorParts.Section(
                    "即時值",
                    null,
                    DataEditorParts.LiveValue(data, "name", "dept")));
        }

        private static FormDataObject BuildData()
        {
            var schema = new FormSchema("Demo", "Demo");
            var master = schema.Tables!.Add("Demo", "Demo");
            master.Fields!.Add("name", "Name", FieldDbType.String);
            var dept = master.Fields.Add("dept", "Department", FieldDbType.String);
            dept.ListItems!.Add("HR", "Human Resources");
            dept.ListItems.Add("IT", "Information Technology");
            dept.ListItems.Add("FIN", "Finance");

            var data = new FormDataObject(schema);
            data.InitializeNewMaster();
            data.SetField("name", "Alice Chen");
            data.SetField("dept", "IT");
            return data;
        }
    }
}
