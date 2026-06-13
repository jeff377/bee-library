using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// 補強 <see cref="FieldEditorBinder"/> 覆蓋率：透過 FormScope 屬性觸發
    /// <c>OnFormModeChanged</c> 路徑（class handler 路徑，有別於直接呼叫
    /// <c>SetControlState</c>）。
    /// </summary>
    public class FieldEditorBinderAdditionalTests
    {
        private static FormDataObject BuildDataObject()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        [Fact]
        [DisplayName("透過 FormScope.FormModeProperty 觸發 OnFormModeChanged，View 模式套用唯讀")]
        public void OnFormModeChanged_ViaFormScopeProperty_ViewMode_SetsReadOnly()
        {
            var dataObject = BuildDataObject();
            var editor = new TextEdit();
            editor.Bind(dataObject, "emp_name");
            editor.SetControlState(SingleFormMode.Edit);
            Assert.False(editor.IsReadOnly);

            // class handler: FormScope.FormModeProperty.Changed.AddClassHandler<TextEdit>(
            //   (o, e) => o._binder.OnFormModeChanged((SingleFormMode)e.NewValue!))
            FormScope.SetFormMode(editor, SingleFormMode.View);

            Assert.True(editor.IsReadOnly);
        }

        [Fact]
        [DisplayName("透過 FormScope.FormModeProperty 切換為 Edit 模式，編輯器可編輯")]
        public void OnFormModeChanged_ViaFormScopeProperty_EditMode_ClearsReadOnly()
        {
            var dataObject = BuildDataObject();
            var editor = new TextEdit();
            editor.Bind(dataObject, "emp_name");
            FormScope.SetFormMode(editor, SingleFormMode.View);
            Assert.True(editor.IsReadOnly);

            FormScope.SetFormMode(editor, SingleFormMode.Edit);

            Assert.False(editor.IsReadOnly);
        }

        [Fact]
        [DisplayName("AllowEditModes=Add 時 Edit 模式透過 FormScope 觸發後仍唯讀")]
        public void OnFormModeChanged_ViaFormScope_AllowEditModesAdd_EditModeReadOnly()
        {
            var dataObject = BuildDataObject();
            var field = new LayoutField { FieldName = "emp_name", AllowEditModes = FormEditModes.Add };
            var editor = new TextEdit();
            editor.Bind(dataObject, field);

            // Add 模式：AllowEditModes.Allows(Add) = true → 可編輯
            FormScope.SetFormMode(editor, SingleFormMode.Add);
            Assert.False(editor.IsReadOnly);

            // Edit 模式：AllowEditModes.Allows(Edit) = false → 唯讀
            FormScope.SetFormMode(editor, SingleFormMode.Edit);
            Assert.True(editor.IsReadOnly);
        }
    }
}
