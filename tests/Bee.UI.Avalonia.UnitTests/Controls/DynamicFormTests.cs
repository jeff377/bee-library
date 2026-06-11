using System.ComponentModel;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Bee.Base.Data;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls;
using Bee.UI.Avalonia.Controls.Editors;
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
        private static readonly Type[] s_buildInputParams = [typeof(LayoutField)];

        private static FormSchema BuildSchema()
        {
            var schema = new FormSchema("Employee", "Employee");
            var master = schema.Tables!.Add("Employee", "Employee");
            master.Fields!.Add("emp_id", "ID", FieldDbType.String);
            master.Fields.Add("is_active", "Active", FieldDbType.Boolean);
            master.Fields.Add("hire_date", "Hire Date", FieldDbType.Date);
            return schema;
        }

        private static Control InvokeBuildInputControl(DynamicForm form, LayoutField field)
        {
            var method = typeof(DynamicForm).GetMethod(
                "BuildInputControl",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null, s_buildInputParams, null);
            Assert.NotNull(method);
            return (Control)method!.Invoke(form, new object[] { field })!;
        }

        private static (DynamicForm Form, FormDataObject DataObject) BuildFormWithDataObjectOnly()
        {
            var form = new DynamicForm();
            // Only DataObject is assigned (FormLayout stays null) so Rebuild() renders
            // nothing and BuildInputControl can be exercised in isolation.
            var dataObject = new FormDataObject(BuildSchema());
            dataObject.InitializeNewMaster();
            form.DataObject = dataObject;
            return (form, dataObject);
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
        [InlineData(ControlType.CheckEdit, typeof(CheckEdit))]
        [InlineData(ControlType.DateEdit, typeof(DateEdit))]
        [InlineData(ControlType.YearMonthEdit, typeof(YearMonthEdit))]
        [InlineData(ControlType.MemoEdit, typeof(MemoEdit))]
        [InlineData(ControlType.DropDownEdit, typeof(DropDownEdit))]
        [InlineData(ControlType.ButtonEdit, typeof(ButtonEdit))]
        [InlineData(ControlType.TextEdit, typeof(TextEdit))]
        [InlineData(ControlType.Auto, typeof(TextEdit))]
        [DisplayName("BuildInputControl 依 ControlType 分派對應的 field editor 並完成綁定")]
        public void BuildInputControl_DispatchesByControlType(ControlType controlType, Type expectedControlType)
        {
            var (form, _) = BuildFormWithDataObjectOnly();
            var field = new LayoutField { FieldName = "emp_id", ControlType = controlType };

            var control = InvokeBuildInputControl(form, field);

            Assert.IsType(expectedControlType, control);
        }

        [Fact]
        [DisplayName("MemoEdit 編輯器接受多行輸入並載入初值")]
        public void BuildInputControl_MemoEdit_AcceptsReturn()
        {
            var (form, dataObject) = BuildFormWithDataObjectOnly();
            dataObject.SetField("emp_id", "some text");
            var field = new LayoutField { FieldName = "emp_id", ControlType = ControlType.MemoEdit };

            var control = Assert.IsType<MemoEdit>(InvokeBuildInputControl(form, field));

            Assert.True(control.AcceptsReturn);
            Assert.Equal("some text", control.Text);
        }

        [Fact]
        [DisplayName("ReadOnly 欄位建立的編輯器為唯讀")]
        public void BuildInputControl_ReadOnlyField_CreatesReadOnlyTextEdit()
        {
            var (form, _) = BuildFormWithDataObjectOnly();
            var field = new LayoutField { FieldName = "emp_id", ReadOnly = true };

            var control = Assert.IsType<TextEdit>(InvokeBuildInputControl(form, field));

            Assert.True(control.IsReadOnly);
        }

        [Fact]
        [DisplayName("DateEdit 以 ISO 日期初始化 SelectedDate")]
        public void BuildInputControl_DateEdit_ParsesInitialValue()
        {
            var (form, dataObject) = BuildFormWithDataObjectOnly();
            dataObject.SetField("hire_date", "2026-05-21");
            var field = new LayoutField { FieldName = "hire_date", ControlType = ControlType.DateEdit };

            var picker = Assert.IsType<DateEdit>(InvokeBuildInputControl(form, field));

            Assert.NotNull(picker.SelectedDate);
            Assert.Equal(new DateTime(2026, 5, 21), picker.SelectedDate!.Value.Date);
        }

        [Fact]
        [DisplayName("CheckEdit 勾選變更會回寫 DataObject 欄位")]
        public void BuildInputControl_CheckEdit_WritesBackToDataObject()
        {
            var (form, dataObject) = BuildFormWithDataObjectOnly();
            var field = new LayoutField { FieldName = "is_active", ControlType = ControlType.CheckEdit };

            var checkBox = Assert.IsType<CheckEdit>(InvokeBuildInputControl(form, field));
            checkBox.IsChecked = true;

            Assert.Equal("True", dataObject.GetField("is_active"));
            Assert.True(dataObject.IsDirty);
        }
    }
}
