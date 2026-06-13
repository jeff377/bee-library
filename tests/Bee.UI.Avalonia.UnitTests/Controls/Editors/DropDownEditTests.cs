using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Collections;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// Behaviour checks for <see cref="DropDownEdit"/>: option loading from
    /// <c>FormField.ListItems</c>, value selection and write-back.
    /// </summary>
    public class DropDownEditTests
    {
        private static FormDataObject BuildDataObject()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            var dept = master.Fields!.Add("dept_id", "Department", FieldDbType.String);
            dept.ListItems!.Add("HR", "Human Resources");
            dept.ListItems.Add("IT", "Information Technology");
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        [Fact]
        [DisplayName("Bind 後自動載入 FormField.ListItems 為選項")]
        public void Bind_FieldWithListItems_LoadsOptions()
        {
            var dataObject = BuildDataObject();
            var editor = new DropDownEdit();

            editor.Bind(dataObject, "dept_id");

            var items = Assert.IsType<IEnumerable<ListItem>>(editor.ItemsSource, false).ToList();
            Assert.Equal(2, items.Count);
            Assert.Equal("HR", items[0].Value);
        }

        [Fact]
        [DisplayName("Bind 後依欄位值選取對應項目")]
        public void Bind_ExistingValue_SelectsMatchingItem()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("dept_id", "IT");

            var editor = new DropDownEdit();
            editor.Bind(dataObject, "dept_id");

            var selected = Assert.IsType<ListItem>(editor.SelectedItem);
            Assert.Equal("IT", selected.Value);
        }

        [Fact]
        [DisplayName("選取變更以 ListItem.Value 寫回")]
        public void SelectionChanged_AfterBind_WritesBackValue()
        {
            var dataObject = BuildDataObject();
            var editor = new DropDownEdit();
            editor.Bind(dataObject, "dept_id");

            editor.SelectedIndex = 0;

            Assert.Equal("HR", dataObject.GetField("dept_id"));
        }

        [Fact]
        [DisplayName("他方 SetField 後自動選取新值")]
        public void FieldValueChanged_OtherWriter_UpdatesSelection()
        {
            var dataObject = BuildDataObject();
            var editor = new DropDownEdit();
            editor.Bind(dataObject, "dept_id");

            dataObject.SetField("dept_id", "HR");

            var selected = Assert.IsType<ListItem>(editor.SelectedItem);
            Assert.Equal("HR", selected.Value);
        }

        [Fact]
        [DisplayName("AllowEditModes=Add 時僅新增模式啟用")]
        public void SetControlState_AllowEditModesAdd_OnlyAddEnabled()
        {
            var dataObject = BuildDataObject();
            var field = new LayoutField { FieldName = "dept_id", AllowEditModes = FormEditModes.Add };
            var editor = new DropDownEdit();
            editor.Bind(dataObject, field);

            editor.SetControlState(SingleFormMode.Add);
            Assert.True(editor.IsEnabled);

            editor.SetControlState(SingleFormMode.Edit);
            Assert.False(editor.IsEnabled);

            editor.SetControlState(SingleFormMode.View);
            Assert.False(editor.IsEnabled);
        }
    }
}
