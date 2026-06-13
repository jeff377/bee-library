using System.ComponentModel;
using System.Data;
using System.Reflection;
using Avalonia.Input;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// 補強 <see cref="ButtonEdit"/> 覆蓋率：ButtonClick 事件（非 lookup 欄）、
    /// Delete/Back 清除 lookup 選取、版面層級 DisplayFields 覆寫。
    /// </summary>
    public class ButtonEditAdditionalTests
    {
        private static FormSchema BuildOrderSchema()
        {
            var schema = new FormSchema("Order", "訂單") { CategoryId = "company" };
            var table = schema.Tables!.Add("Order", "訂單");
            table.Fields!.Add(new FormField(SysFields.RowId, "唯一識別", FieldDbType.Guid));
            table.Fields!.Add(new FormField(SysFields.Id, "單號", FieldDbType.String));

            var customerField = new FormField("customer_rowid", "客戶", FieldDbType.Guid)
            {
                RelationProgId = "Customer",
            };
            customerField.RelationFieldMappings!.Add(SysFields.Id, "ref_customer_id");
            customerField.RelationFieldMappings!.Add(SysFields.Name, "ref_customer_name");
            table.Fields!.Add(customerField);
            table.Fields!.Add(new FormField("ref_customer_id", "客戶代碼", FieldDbType.String, FieldType.RelationField));
            table.Fields!.Add(new FormField("ref_customer_name", "客戶名稱", FieldDbType.String, FieldType.RelationField));
            return schema;
        }

        private static DataRow BuildSelectedCustomerRow(Guid rowId, string id, string name)
        {
            var table = new DataTable("Customer");
            table.Columns.Add(SysFields.RowId, typeof(Guid));
            table.Columns.Add(SysFields.Id, typeof(string));
            table.Columns.Add(SysFields.Name, typeof(string));
            table.Rows.Add(rowId, id, name);
            return table.Rows[0];
        }

        private static FormDataObject BuildOrderDataObject()
        {
            var schema = BuildOrderSchema();
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        private static void InvokeOnKeyDown(ButtonEdit editor, Key key)
        {
            var method = typeof(ButtonEdit).GetMethod(
                "OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var args = new KeyEventArgs { Key = key };
            method!.Invoke(editor, new object[] { args });
        }

        private static async Task InvokeOnButtonClickAsync(ButtonEdit editor)
        {
            var method = typeof(ButtonEdit).GetMethod(
                "OnButtonClickAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            await (Task)method!.Invoke(editor, null)!;
        }

        [Fact]
        [DisplayName("非 lookup 欄位點擊按鈕觸發 ButtonClick 事件")]
        public async Task OnButtonClickAsync_NonLookupField_RaisesButtonClickEvent()
        {
            var schema = BuildOrderSchema();
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var editor = new ButtonEdit();
            editor.Bind(dataObject, SysFields.Id);
            Assert.False(editor.HasLookup);

            var invoked = false;
            editor.ButtonClick += (_, _) => invoked = true;

            await InvokeOnButtonClickAsync(editor);

            Assert.True(invoked);
        }

        [Fact]
        [DisplayName("Delete 鍵在 lookup 允許編輯模式下清除選取值")]
        public void OnKeyDown_Delete_LookupModeAllowEdit_ClearsSelection()
        {
            var dataObject = BuildOrderDataObject();
            var layout = BuildOrderSchema().GetFormLayout();
            var layoutField = layout.Sections![0].Fields!.First(f => f.FieldName == "customer_rowid");
            var editor = new ButtonEdit();
            editor.Bind(dataObject, layoutField);
            editor.SetControlState(SingleFormMode.Edit);
            Assert.True(editor.HasLookup);

            var field = dataObject.GetFormField("customer_rowid")!;
            dataObject.ApplyLookupSelection(field, BuildSelectedCustomerRow(Guid.NewGuid(), "C001", "客戶甲"));
            Assert.False(string.IsNullOrEmpty(editor.Text));

            InvokeOnKeyDown(editor, Key.Delete);

            Assert.Equal(string.Empty, editor.Text);
        }

        [Fact]
        [DisplayName("Back 鍵同樣清除 lookup 選取值")]
        public void OnKeyDown_Back_LookupModeAllowEdit_ClearsSelection()
        {
            var dataObject = BuildOrderDataObject();
            var layout = BuildOrderSchema().GetFormLayout();
            var layoutField = layout.Sections![0].Fields!.First(f => f.FieldName == "customer_rowid");
            var editor = new ButtonEdit();
            editor.Bind(dataObject, layoutField);
            editor.SetControlState(SingleFormMode.Edit);

            var field = dataObject.GetFormField("customer_rowid")!;
            dataObject.ApplyLookupSelection(field, BuildSelectedCustomerRow(Guid.NewGuid(), "C001", "客戶甲"));

            InvokeOnKeyDown(editor, Key.Back);

            Assert.Equal(string.Empty, editor.Text);
        }

        [Fact]
        [DisplayName("版面層級 DisplayFields 覆寫 schema 預設，僅顯示指定欄位")]
        public void RefreshFromSource_WithLayoutDisplayFields_UsesLayoutOverride()
        {
            var schema = BuildOrderSchema();
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();

            // 版面只顯示客戶代碼，不顯示名稱
            var layoutField = new LayoutField
            {
                FieldName = "customer_rowid",
                DisplayFields = "ref_customer_id",
            };
            var editor = new ButtonEdit();
            editor.Bind(dataObject, layoutField);
            editor.SetControlState(SingleFormMode.Edit);
            Assert.True(editor.HasLookup);

            var field = dataObject.GetFormField("customer_rowid")!;
            dataObject.ApplyLookupSelection(
                field,
                BuildSelectedCustomerRow(Guid.NewGuid(), "C001", "客戶甲"));

            // 版面覆寫後只取 ref_customer_id，不含名稱
            Assert.Equal("C001", editor.Text);
        }
    }
}
