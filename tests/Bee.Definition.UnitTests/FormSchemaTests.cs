using System.ComponentModel;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Base.Data;
using Bee.Definition.Database;

namespace Bee.Definition.UnitTests
{
    [Collection("Initialize")]
    public class FormSchemaTests
    {
        [Fact]
        [DisplayName("FormSchema 建立部門表單定義應包含有效的主檔表")]
        public void CreateFormSchema_DepartmentWithRelations_HasMasterTable()
        {
            var formSchema = new FormSchema("Department", "部門");
            var table = formSchema.Tables!.Add("Department", "部門");
            table.DbTableName = "ft_department";
            table.Fields!.Add("sys_no", "流水號", FieldDbType.AutoIncrement);
            table.Fields!.Add("sys_rowid", "唯一識別", FieldDbType.Guid);
            table.Fields!.Add("sys_id", "部門編號", FieldDbType.String);
            table.Fields!.Add("sys_name", "部門名稱", FieldDbType.String);
            var managerField = new FormField("manager_rowid", "部門主管唯一識別", FieldDbType.String)
            {
                RelationProgId = "Employee"
            };
            managerField.RelationFieldMappings!.Add("sys_id", "ref_manager_id");
            managerField.RelationFieldMappings!.Add("sys_name", "ref_manager_name");
            table.Fields!.Add(managerField);
            table.Fields!.Add(new FormField("ref_manager_id", "部門主管編號", FieldDbType.String, FieldType.RelationField));
            table.Fields!.Add(new FormField("ref_manager_name", "部門主管名稱", FieldDbType.String, FieldType.RelationField));

            Assert.NotNull(formSchema.MasterTable);
        }

        [Fact]
        [DisplayName("FormSchema 建立員工表單定義應包含關聯欄位參考")]
        public void CreateFormSchema_EmployeeWithRelations_HasRelationFieldReferences()
        {
            var formSchema = new FormSchema("Employee", "員工");
            var table = formSchema.Tables!.Add("Employee", "員工");
            table.DbTableName = "ft_employee";
            table.Fields!.Add("sys_no", "流水號", FieldDbType.AutoIncrement);
            table.Fields!.Add("sys_rowid", "唯一識別", FieldDbType.Guid);
            table.Fields!.Add("sys_id", "員工編號", FieldDbType.String);
            table.Fields!.Add("sys_name", "員工姓名", FieldDbType.String);
            var deptField = new FormField("dept_rowid", "部門唯一識別", FieldDbType.String)
            {
                RelationProgId = "Department"
            };
            deptField.RelationFieldMappings!.Add("sys_id", "ref_dept_id");
            deptField.RelationFieldMappings!.Add("sys_name", "ref_dept_name");
            deptField.RelationFieldMappings!.Add("ref_manager_id", "ref_supervisor_id");
            deptField.RelationFieldMappings!.Add("ref_manager_name", "ref_supervisor_name");
            table.Fields!.Add(deptField);
            table.Fields!.Add(new FormField("ref_dept_id", "部門編號", FieldDbType.String, FieldType.RelationField));
            table.Fields!.Add(new FormField("ref_dept_name", "部門名稱", FieldDbType.String, FieldType.RelationField));
            table.Fields!.Add(new FormField("ref_supervisor_id", "直屬主管編號", FieldDbType.String, FieldType.RelationField));
            table.Fields!.Add(new FormField("ref_supervisor_name", "直屬主管名稱", FieldDbType.String, FieldType.RelationField));

            var references = table.RelationFieldReferences;

            Assert.NotNull(references);
        }

        [Fact]
        [DisplayName("GetListLayout 應包含隱藏的 RowID 欄位並依 ListFields 加入顯示欄")]
        public void GetListLayout_ValidSchema_ContainsRowIdAndListFields()
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "sys_id,sys_name" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add("sys_rowid", "唯一識別", FieldDbType.Guid);
            table.Fields!.Add(new FormField("sys_id", "編號", FieldDbType.String) { Width = 150 });
            table.Fields!.Add(new FormField("sys_name", "名稱", FieldDbType.String));

            var grid = schema.GetListLayout();

            Assert.Equal("Demo", grid.TableName);
            Assert.Equal(3, grid.Columns!.Count);

            var rowIdColumn = grid.Columns[0];
            Assert.Equal(SysFields.RowId, rowIdColumn.FieldName);
            Assert.False(rowIdColumn.Visible);

            var idColumn = grid.Columns[1];
            Assert.Equal("sys_id", idColumn.FieldName);
            Assert.Equal(150, idColumn.Width);

            var nameColumn = grid.Columns[2];
            Assert.Equal("sys_name", nameColumn.FieldName);
            Assert.Equal(120, nameColumn.Width);
        }

        [Theory]
        [InlineData(ControlType.TextEdit, ColumnControlType.TextEdit)]
        [InlineData(ControlType.ButtonEdit, ColumnControlType.ButtonEdit)]
        [InlineData(ControlType.DateEdit, ColumnControlType.DateEdit)]
        [InlineData(ControlType.YearMonthEdit, ColumnControlType.YearMonthEdit)]
        [InlineData(ControlType.DropDownEdit, ColumnControlType.DropDownEdit)]
        [InlineData(ControlType.CheckEdit, ColumnControlType.CheckEdit)]
        [InlineData(ControlType.Auto, ColumnControlType.TextEdit)]
        [InlineData(ControlType.MemoEdit, ColumnControlType.TextEdit)]
        [DisplayName("GetListLayout 應依 ControlType 轉換為對應的 ColumnControlType（Auto/MemoEdit 退化為 TextEdit）")]
        public void GetListLayout_ControlType_ConvertsToColumnControlType(
            ControlType controlType, ColumnControlType expected)
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "col" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("col", "欄", FieldDbType.String) { ControlType = controlType });

            var grid = schema.GetListLayout();

            var column = grid.Columns!.First(c => c.FieldName == "col");
            Assert.Equal(expected, column.ControlType);
        }

        [Fact]
        [DisplayName("GetListLayout Width=0 時應套用預設寬度 120")]
        public void GetListLayout_WidthZero_UsesDefault120()
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "col" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("col", "欄", FieldDbType.String));

            var grid = schema.GetListLayout();

            Assert.Equal(120, grid.Columns!.First(c => c.FieldName == "col").Width);
        }

        [Fact]
        [DisplayName("GetListLayout 應將 DisplayFormat 與 NumberFormat 傳遞至 LayoutColumn")]
        public void GetListLayout_PropagatesDisplayAndNumberFormats()
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "amount" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("amount", "金額", FieldDbType.Decimal)
            {
                DisplayFormat = "{0:C}",
                NumberFormat = "Amount"
            });

            var grid = schema.GetListLayout();

            var column = grid.Columns!.First(c => c.FieldName == "amount");
            Assert.Equal("{0:C}", column.DisplayFormat);
            Assert.Equal("Amount", column.NumberFormat);
        }

        [Fact]
        [DisplayName("GetListLayout ListFields 含不存在欄位時應拋 KeyNotFoundException")]
        public void GetListLayout_UnknownFieldInListFields_ThrowsKeyNotFoundException()
        {
            // FormFieldCollection 的 indexer 在找不到欄位時直接拋 KeyNotFoundException;
            // FormSchema.GetListLayout 後續的 null 檢查實際上是不可達的 dead code。
            var schema = new FormSchema("Demo", "示範") { ListFields = "known,missing" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField("known", "已知", FieldDbType.String));

            Assert.Throws<KeyNotFoundException>(() => schema.GetListLayout());
        }
    }
}
