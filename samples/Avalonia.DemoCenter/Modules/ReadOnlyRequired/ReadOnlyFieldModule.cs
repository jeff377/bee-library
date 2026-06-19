using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;
using Avalonia.DemoCenter.Modules.DataEditors;

namespace Avalonia.DemoCenter.Modules.ReadOnlyRequired
{
    /// <summary>
    /// Per-field read-only via <c>LayoutField.ReadOnly</c>: a read-only field renders with
    /// the "de-framed, underline-only" appearance (CheckEdit greys the box but keeps the
    /// label readable), independent of the form's FormMode.
    /// </summary>
    public sealed class ReadOnlyFieldModule : DemoModuleBase
    {
        /// <inheritdoc/>
        public override string Category => "唯讀與必填";

        /// <inheritdoc/>
        public override string Title => "LayoutField.ReadOnly";

        /// <inheritdoc/>
        public override string Description =>
            "以 LayoutField.ReadOnly=true 逐欄設定永久唯讀：去框留底線、CheckEdit 灰框留字；無論 FormMode 為何都唯讀。";

        /// <inheritdoc/>
        public override Control BuildView()
        {
            var data = BuildData();

            return DataEditorParts.Compose(
                data,
                DataEditorParts.Section(
                    "一般（依 FormMode 可編輯）",
                    "ambient 綁定；工具列 FormMode 切 View 時也會轉唯讀。",
                    DataEditorParts.LabeledRow("name", new TextEdit { FieldName = "name" }),
                    DataEditorParts.LabeledRow("hire_date", new DateEdit { FieldName = "hire_date" }),
                    DataEditorParts.LabeledRow("dept", new DropDownEdit { FieldName = "dept" }),
                    DataEditorParts.LabeledRow("active", new CheckEdit { FieldName = "active", Content = "Active" })),
                DataEditorParts.Section(
                    "唯讀（LayoutField.ReadOnly=true）",
                    "永久唯讀：文字 / 日期 / 下拉去框留底線，CheckEdit 灰框留字。",
                    DataEditorParts.LabeledRow("name", ReadOnly(new TextEdit(), data, "name")),
                    DataEditorParts.LabeledRow("hire_date", ReadOnly(new DateEdit(), data, "hire_date")),
                    DataEditorParts.LabeledRow("dept", ReadOnly(new DropDownEdit(), data, "dept")),
                    DataEditorParts.LabeledRow("active", ReadOnlyCheck(data, "active"))));
        }

        private static T ReadOnly<T>(T editor, FormDataObject data, string field)
            where T : Control, IFieldEditor
        {
            editor.Bind(data, new LayoutField { FieldName = field, ReadOnly = true });
            return editor;
        }

        private static CheckEdit ReadOnlyCheck(FormDataObject data, string field)
        {
            var editor = new CheckEdit { Content = "Active" };
            editor.Bind(data, new LayoutField { FieldName = field, ReadOnly = true });
            return editor;
        }

        private static FormDataObject BuildData()
        {
            var schema = new FormSchema("Demo", "Demo");
            var master = schema.Tables!.Add("Demo", "Demo");
            master.Fields!.Add("name", "Name", FieldDbType.String);
            master.Fields.Add("hire_date", "Hire Date", FieldDbType.Date);
            var dept = master.Fields.Add("dept", "Department", FieldDbType.String);
            dept.ListItems!.Add("HR", "Human Resources");
            dept.ListItems.Add("IT", "Information Technology");
            dept.ListItems.Add("FIN", "Finance");
            master.Fields.Add("active", "Active", FieldDbType.Boolean);

            var data = new FormDataObject(schema);
            data.InitializeNewMaster();
            data.SetField("name", "Alice Chen");
            data.SetField("hire_date", "2026-06-11");
            data.SetField("dept", "IT");
            data.SetField("active", bool.TrueString);
            return data;
        }
    }
}
