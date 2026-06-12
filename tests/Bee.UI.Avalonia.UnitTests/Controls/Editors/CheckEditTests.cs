using System.ComponentModel;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls.Editors
{
    /// <summary>
    /// Behaviour checks for <see cref="CheckEdit"/>: boolean round-trip and
    /// form-mode state.
    /// </summary>
    public class CheckEditTests
    {
        private static FormDataObject BuildDataObject()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("is_active", "Active", FieldDbType.Boolean);
            var dataObject = new FormDataObject(schema);
            dataObject.InitializeNewMaster();
            return dataObject;
        }

        [Fact]
        [DisplayName("Bind 後載入布林初值")]
        public void Bind_TrueValue_LoadsIntoIsChecked()
        {
            var dataObject = BuildDataObject();
            dataObject.SetField("is_active", bool.TrueString);

            var editor = new CheckEdit();
            editor.Bind(dataObject, "is_active");

            Assert.True(editor.IsChecked);
        }

        [Fact]
        [DisplayName("勾選變更寫回 FormDataObject")]
        public void IsCheckedChanged_AfterBind_WritesBack()
        {
            var dataObject = BuildDataObject();
            var editor = new CheckEdit();
            editor.Bind(dataObject, "is_active");

            editor.IsChecked = true;

            Assert.Equal("True", dataObject.GetField("is_active"));
        }

        [Fact]
        [DisplayName("SetControlState View 模式停用，Edit 模式啟用")]
        public void SetControlState_ViewMode_TogglesIsEnabled()
        {
            var dataObject = BuildDataObject();
            var editor = new CheckEdit();
            editor.Bind(dataObject, "is_active");

            editor.SetControlState(SingleFormMode.View);
            Assert.False(editor.IsEnabled);

            editor.SetControlState(SingleFormMode.Edit);
            Assert.True(editor.IsEnabled);
        }

        [Fact]
        [DisplayName("AllowEditModes=Add 時僅新增模式啟用")]
        public void SetControlState_AllowEditModesAdd_OnlyAddEnabled()
        {
            var dataObject = BuildDataObject();
            var field = new LayoutField { FieldName = "is_active", AllowEditModes = FormEditModes.Add };
            var editor = new CheckEdit();
            editor.Bind(dataObject, field);

            editor.SetControlState(SingleFormMode.Add);
            Assert.True(editor.IsEnabled);

            editor.SetControlState(SingleFormMode.Edit);
            Assert.False(editor.IsEnabled);

            editor.SetControlState(SingleFormMode.View);
            Assert.False(editor.IsEnabled);
        }

        [Fact]
        [DisplayName("FieldValue 接受 bool 與字串表示")]
        public void FieldValue_BoolAndString_MapToIsChecked()
        {
            var editor = new CheckEdit();

            editor.FieldValue = true;
            Assert.True(editor.IsChecked);

            editor.FieldValue = "false";
            Assert.False(editor.IsChecked);
        }
    }
}
