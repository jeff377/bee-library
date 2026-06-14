using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// 補強 <see cref="ButtonEdit"/> 覆蓋率：非 lookup 欄位的 SetControlState（呼叫 base），
    /// OnPropertyChanged 在 IsReadOnly 變更時同步按鈕啟用狀態，
    /// 版面層級 DisplayFields 覆寫 ResolveDisplayFieldNames 路徑。
    /// </summary>
    public class ButtonEditPropertyTests
    {
        private static Button GetButton(ButtonEdit editor)
        {
            var field = typeof(ButtonEdit).GetField("_button", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return (Button)field!.GetValue(editor)!;
        }

        private static FormSchema BuildOrderSchema()
        {
            var schema = new FormSchema("Order", "Order");
            var table = schema.Tables!.Add("Order", "Order");
            table.Fields!.Add(new FormField("order_id", "Order ID", FieldDbType.String));
            var customerField = new FormField("customer_rowid", "Customer", FieldDbType.Guid)
            {
                RelationProgId = "Customer",
            };
            customerField.RelationFieldMappings!.Add("sys_id", "ref_customer_id");
            customerField.RelationFieldMappings!.Add("sys_name", "ref_customer_name");
            table.Fields!.Add(customerField);
            table.Fields!.Add(new FormField("ref_customer_id", "Customer Code", FieldDbType.String,
                Bee.Definition.Database.FieldType.RelationField));
            table.Fields!.Add(new FormField("ref_customer_name", "Customer Name", FieldDbType.String,
                Bee.Definition.Database.FieldType.RelationField));
            return schema;
        }

        [Fact]
        [DisplayName("非 lookup 欄位 SetControlState Edit 模式：IsReadOnly=false，按鈕啟用")]
        public void SetControlState_NonLookup_EditMode_IsReadOnlyFalseButtonEnabled()
        {
            var schema = BuildOrderSchema();
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var editor = new ButtonEdit();
            editor.Bind(dataObject, "order_id");
            Assert.False(editor.HasLookup);

            editor.SetControlState(SingleFormMode.Edit);

            Assert.False(editor.IsReadOnly);
            Assert.True(GetButton(editor).IsEnabled);
        }

        [Fact]
        [DisplayName("非 lookup 欄位 SetControlState View 模式：IsReadOnly=true，按鈕停用")]
        public void SetControlState_NonLookup_ViewMode_IsReadOnlyTrueButtonDisabled()
        {
            var schema = BuildOrderSchema();
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            var editor = new ButtonEdit();
            editor.Bind(dataObject, "order_id");
            editor.SetControlState(SingleFormMode.Edit);

            editor.SetControlState(SingleFormMode.View);

            Assert.True(editor.IsReadOnly);
            Assert.False(GetButton(editor).IsEnabled);
        }

        [Fact]
        [DisplayName("非 lookup 欄位直接設定 IsReadOnly 後按鈕啟用狀態同步（OnPropertyChanged 路徑）")]
        public void IsReadOnly_SetTrue_NonLookup_ButtonBecomesDisabled()
        {
            var editor = new ButtonEdit();
            editor.IsReadOnly = false;

            editor.IsReadOnly = true;

            Assert.False(GetButton(editor).IsEnabled);
        }

        [Fact]
        [DisplayName("版面層級 DisplayFields 覆寫時 Text 只顯示版面指定的欄位値")]
        public void RefreshFromSource_LayoutDisplayFieldsOverride_ShowsOnlyLayoutFields()
        {
            var schema = BuildOrderSchema();
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            dataObject.SetField("ref_customer_id", "C001");
            dataObject.SetField("ref_customer_name", "Alice");

            var layoutField = new LayoutField
            {
                FieldName = "customer_rowid",
                DisplayFields = "ref_customer_name",
            };
            var editor = new ButtonEdit();
            editor.Bind(dataObject, layoutField);

            Assert.Equal("Alice", editor.Text);
        }
    }
}
