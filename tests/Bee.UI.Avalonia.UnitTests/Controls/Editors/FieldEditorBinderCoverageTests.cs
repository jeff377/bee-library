using System.ComponentModel;
using System.Reflection;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// 補強 <see cref="FieldEditorBinder"/> 的 ambient 綁定路徑覆蓋率：透過反射對
    /// <see cref="TextEdit"/> 的私有 _binder 呼叫 NotifyAttached / NotifyDetached，
    /// 模擬 OnAttachedToLogicalTree / OnDetachedFromLogicalTree 的觸發路徑。
    /// 同時驗證 OnBindingContextChanged 在已附加後切換 DataObject 的重新綁定行為。
    /// </summary>
    public class FieldEditorBinderCoverageTests
    {
        private static FormDataObject BuildDataObject(string empName = "Alice")
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_name", "Name", FieldDbType.String);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            dataObject.SetField("emp_name", empName);
            return dataObject;
        }

        private static object GetBinder(TextEdit editor)
        {
            var field = typeof(TextEdit).GetField("_binder", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return field!.GetValue(editor)!;
        }

        private static void InvokeNotifyAttached(object binder)
        {
            var method = binder.GetType().GetMethod("NotifyAttached");
            Assert.NotNull(method);
            method!.Invoke(binder, null);
        }

        private static void InvokeNotifyDetached(object binder)
        {
            var method = binder.GetType().GetMethod("NotifyDetached");
            Assert.NotNull(method);
            method!.Invoke(binder, null);
        }

        [Fact]
        [DisplayName("NotifyAttached：ambient DataObject + FieldName 已設定時自動綁定編輯器並載入初值")]
        public void NotifyAttached_WithAmbientDataObjectAndFieldName_BindsEditorAndLoadsValue()
        {
            var dataObject = BuildDataObject("Alice");
            var editor = new TextEdit();
            editor.FieldName = "emp_name";
            FormScope.SetDataObject(editor, dataObject);

            InvokeNotifyAttached(GetBinder(editor));

            Assert.Equal("Alice", editor.Text);
        }

        [Fact]
        [DisplayName("NotifyAttached：ambient DataObject 未設定時不綁定，Text 維持空字串")]
        public void NotifyAttached_NoAmbientDataObject_DoesNotBind()
        {
            var editor = new TextEdit();
            editor.FieldName = "emp_name";

            InvokeNotifyAttached(GetBinder(editor));

            Assert.Equal(string.Empty, editor.Text);
        }

        [Fact]
        [DisplayName("NotifyDetached：ambient 綁定後呼叫 NotifyDetached，後續 SetField 不再刷新編輯器")]
        public void NotifyDetached_AfterAmbientBind_StopsUpdates()
        {
            var dataObject = BuildDataObject("Alice");
            var editor = new TextEdit();
            editor.FieldName = "emp_name";
            FormScope.SetDataObject(editor, dataObject);
            var binder = GetBinder(editor);
            InvokeNotifyAttached(binder);
            Assert.Equal("Alice", editor.Text);

            InvokeNotifyDetached(binder);

            dataObject.SetField("emp_name", "Bob");
            // 已解除綁定 → 刷新事件不再路由至編輯器
            Assert.Equal("Alice", editor.Text);
        }

        [Fact]
        [DisplayName("OnBindingContextChanged：附加後切換 DataObject，編輯器重新綁定到新 DataObject")]
        public void OnBindingContextChanged_AfterAttach_NewDataObject_Rebinds()
        {
            var dataObject1 = BuildDataObject("Alice");
            var dataObject2 = BuildDataObject("Bob");

            var editor = new TextEdit();
            editor.FieldName = "emp_name";
            FormScope.SetDataObject(editor, dataObject1);
            InvokeNotifyAttached(GetBinder(editor));
            Assert.Equal("Alice", editor.Text);

            // 切換 DataObject → 觸發 DataObjectProperty.Changed 類別 handler → OnBindingContextChanged → 重新綁定
            FormScope.SetDataObject(editor, dataObject2);

            Assert.Equal("Bob", editor.Text);
        }

        [Fact]
        [DisplayName("OnBindingContextChanged：相同 DataObject + 相同 FieldName，不重新綁定（短路）")]
        public void OnBindingContextChanged_SameDataObjectAndFieldName_IsNoOp()
        {
            var dataObject = BuildDataObject("Carol");
            var editor = new TextEdit();
            editor.FieldName = "emp_name";
            FormScope.SetDataObject(editor, dataObject);
            InvokeNotifyAttached(GetBinder(editor));
            Assert.Equal("Carol", editor.Text);

            editor.Text = "Carol-modified";

            // 再次設定相同 DataObject → short-circuit，不重新載入初值，Text 維持修改後值
            FormScope.SetDataObject(editor, dataObject);
            Assert.Equal("Carol-modified", editor.Text);
        }
    }
}
