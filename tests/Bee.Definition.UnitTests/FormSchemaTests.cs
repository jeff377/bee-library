using System.ComponentModel;
using Bee.Definition.Forms;
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
        [DisplayName("GetListLayout 應依 ListFields 順序加入 Columns 並補入隱藏 sys_rowid")]
        public void GetListLayout_ValidSchema_ContainsListFieldsAndHiddenRowId()
        {
            var schema = new FormSchema("Demo", "示範") { ListFields = "sys_id,sys_name" };
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add(new FormField(SysFields.RowId, "唯一識別", FieldDbType.Guid) { Visible = false });
            table.Fields!.Add(new FormField("sys_id", "編號", FieldDbType.String) { Width = 150 });
            table.Fields!.Add(new FormField("sys_name", "名稱", FieldDbType.String));

            var grid = schema.GetListLayout();

            Assert.Equal("Demo", grid.TableName);
            Assert.Equal(3, grid.Columns!.Count);

            // ListFields 指定的順序在前，sys_rowid 補在最後
            Assert.Equal("sys_id", grid.Columns![0].FieldName);
            Assert.Equal(150, grid.Columns![0].Width);
            Assert.Equal("sys_name", grid.Columns![1].FieldName);
            Assert.Equal(0, grid.Columns![1].Width);

            var rowIdColumn = grid.Columns![2];
            Assert.Equal(SysFields.RowId, rowIdColumn.FieldName);
            Assert.False(rowIdColumn.Visible);
        }

        [Fact]
        [DisplayName("GetFormLayout 對稱於 GetListLayout 應透過 FormLayoutGenerator 產生 FormLayout")]
        public void GetFormLayout_DelegatesToFormLayoutGenerator()
        {
            var schema = new FormSchema("Demo", "示範");
            var table = schema.Tables!.Add("Demo", "示範");
            table.Fields!.Add("sys_id", "編號", FieldDbType.String);

            var layout = schema.GetFormLayout("default");

            Assert.NotNull(layout);
            Assert.Equal("default", layout.LayoutId);
            Assert.Equal("Demo", layout.ProgId);
        }
    }
}
