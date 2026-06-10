using System.ComponentModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.UnitTests.Controls
{
    /// <summary>
    /// Structural + behaviour checks for <see cref="DynamicForm"/>. Mirrors the MAUI
    /// <c>DynamicFormTests</c> pattern: public surface, StyledProperty registration,
    /// and the <c>BuildInputControl</c> ControlType dispatch.
    /// </summary>
    public class DynamicFormTests
    {
        private static readonly Type[] s_buildInputParams = [typeof(LayoutField), typeof(string)];

        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_id", "ID", FieldDbType.String);
            master.Fields.Add("is_active", "Active", FieldDbType.Boolean);
            master.Fields.Add("hire_date", "Hire Date", FieldDbType.Date);
            return schema;
        }

        private static Control InvokeBuildInputControl(DynamicForm form, LayoutField field, string rawValue)
        {
            var method = typeof(DynamicForm).GetMethod(
                "BuildInputControl",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null, s_buildInputParams, null);
            Assert.NotNull(method);
            return (Control)method!.Invoke(form, new object[] { field, rawValue })!;
        }

        private static DynamicForm BuildFormWithDataObjectOnly()
        {
            var form = new DynamicForm();
            // Only DataObject is assigned (FormLayout stays null) so Rebuild() renders
            // nothing and BuildInputControl can be exercised in isolation.
            var dataObject = new FormDataObject(BuildSchema());
            dataObject.InitializeNewMaster();
            form.DataObject = dataObject;
            return form;
        }

        [Fact]
        [DisplayName("DynamicForm 為 Avalonia UserControl 子類別")]
        public void Type_IsUserControlSubclass()
        {
            Assert.True(typeof(UserControl).IsAssignableFrom(typeof(DynamicForm)));
        }

        [Theory]
        [InlineData(nameof(DynamicForm.FormLayout), "FormLayoutProperty")]
        [InlineData(nameof(DynamicForm.DataObject), "DataObjectProperty")]
        [DisplayName("公開屬性皆有對應的 StyledProperty 註冊")]
        public void PublicProperties_HaveMatchingStyledProperty(string propertyName, string styledPropertyFieldName)
        {
            var property = typeof(DynamicForm).GetProperty(
                propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);

            var styled = typeof(DynamicForm).GetField(
                styledPropertyFieldName,
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(styled);
            Assert.True(typeof(AvaloniaProperty).IsAssignableFrom(styled!.FieldType));
            Assert.NotNull(styled.GetValue(null));
        }

        [Fact]
        [DisplayName("可實例化 DynamicForm 並透過 StyledProperty 指派 FormLayout/DataObject")]
        public void CanInstantiateAndAssignParameters()
        {
            var schema = BuildSchema();
            var layout = schema.GetFormLayout();
            var dataObject = new FormDataObject(schema);

            var component = new DynamicForm
            {
                FormLayout = layout,
                DataObject = dataObject,
            };

            Assert.Same(layout, component.FormLayout);
            Assert.Same(dataObject, component.DataObject);
        }

        [Fact]
        [DisplayName("指派 FormLayout + DataObject 後 Content 為每個 Section 一個 Border")]
        public void AssignedLayoutAndDataObject_RendersOneBorderPerSection()
        {
            var schema = BuildSchema();
            var layout = schema.GetFormLayout();
            Assert.NotNull(layout.Sections);
            Assert.NotEmpty(layout.Sections!);

            var component = new DynamicForm
            {
                FormLayout = layout,
                DataObject = new FormDataObject(schema),
            };

            var host = Assert.IsType<StackPanel>(component.Content);
            Assert.Equal(layout.Sections!.Count, host.Children.Count);
            Assert.All(host.Children, child => Assert.IsType<Border>(child));
        }

        [Fact]
        [DisplayName("欄位數超過 ColumnCount 時 Grid 換行配置")]
        public void FieldGrid_WrapsWhenFieldsExceedColumnCount()
        {
            var layout = new FormLayout { ColumnCount = 2 };
            var section = new LayoutSection { Caption = "Main", ShowCaption = false };
            section.Fields!.Add(new LayoutField { FieldName = "emp_id", Caption = "ID" });
            section.Fields.Add(new LayoutField { FieldName = "is_active", Caption = "Active" });
            section.Fields.Add(new LayoutField { FieldName = "hire_date", Caption = "Hire Date" });
            layout.Sections!.Add(section);

            var component = new DynamicForm
            {
                FormLayout = layout,
                DataObject = new FormDataObject(BuildSchema()),
            };

            var host = Assert.IsType<StackPanel>(component.Content);
            var border = Assert.IsType<Border>(host.Children[0]);
            var sectionStack = Assert.IsType<StackPanel>(border.Child);
            var grid = Assert.IsType<Grid>(sectionStack.Children[^1]);

            Assert.Equal(2, grid.ColumnDefinitions.Count);
            // Three fields in a two-column layout require a second row.
            Assert.Equal(2, grid.RowDefinitions.Count);
            Assert.Equal(3, grid.Children.Count);
        }

        [Theory]
        [InlineData(ControlType.CheckEdit, typeof(CheckBox))]
        [InlineData(ControlType.DateEdit, typeof(DatePicker))]
        [InlineData(ControlType.YearMonthEdit, typeof(DatePicker))]
        [InlineData(ControlType.MemoEdit, typeof(TextBox))]
        [InlineData(ControlType.DropDownEdit, typeof(ComboBox))]
        [InlineData(ControlType.TextEdit, typeof(TextBox))]
        [DisplayName("BuildInputControl 依 ControlType 分派對應的 Avalonia 控件")]
        public void BuildInputControl_DispatchesByControlType(ControlType controlType, Type expectedControlType)
        {
            var form = BuildFormWithDataObjectOnly();
            var field = new LayoutField { FieldName = "emp_id", ControlType = controlType };

            var control = InvokeBuildInputControl(form, field, string.Empty);

            Assert.IsType(expectedControlType, control);
        }

        [Fact]
        [DisplayName("MemoEdit 建立的 TextBox 接受多行輸入")]
        public void BuildInputControl_MemoEdit_AcceptsReturn()
        {
            var form = BuildFormWithDataObjectOnly();
            var field = new LayoutField { FieldName = "emp_id", ControlType = ControlType.MemoEdit };

            var control = Assert.IsType<TextBox>(InvokeBuildInputControl(form, field, "some text"));

            Assert.True(control.AcceptsReturn);
            Assert.Equal("some text", control.Text);
        }

        [Fact]
        [DisplayName("ReadOnly 欄位建立的 TextBox 為唯讀")]
        public void BuildInputControl_ReadOnlyField_CreatesReadOnlyTextBox()
        {
            var form = BuildFormWithDataObjectOnly();
            var field = new LayoutField { FieldName = "emp_id", ReadOnly = true };

            var control = Assert.IsType<TextBox>(InvokeBuildInputControl(form, field, "E001"));

            Assert.True(control.IsReadOnly);
        }

        [Fact]
        [DisplayName("DateEdit 以 ISO 日期初始化 DatePicker.SelectedDate")]
        public void BuildInputControl_DateEdit_ParsesInitialValue()
        {
            var form = BuildFormWithDataObjectOnly();
            var field = new LayoutField { FieldName = "hire_date", ControlType = ControlType.DateEdit };

            var picker = Assert.IsType<DatePicker>(InvokeBuildInputControl(form, field, "2026-05-21"));

            Assert.NotNull(picker.SelectedDate);
            Assert.Equal(new DateTime(2026, 5, 21), picker.SelectedDate!.Value.Date);
        }

        [Fact]
        [DisplayName("CheckBox 勾選變更會回寫 DataObject 欄位")]
        public void BuildInputControl_CheckEdit_WritesBackToDataObject()
        {
            var dataObject = new FormDataObject(BuildSchema());
            dataObject.InitializeNewMaster();
            var form = new DynamicForm { DataObject = dataObject };
            var field = new LayoutField { FieldName = "is_active", ControlType = ControlType.CheckEdit };

            var checkBox = Assert.IsType<CheckBox>(InvokeBuildInputControl(form, field, "False"));
            checkBox.IsChecked = true;

            Assert.Equal("True", dataObject.GetField("is_active"));
            Assert.True(dataObject.IsDirty);
        }
    }
}
