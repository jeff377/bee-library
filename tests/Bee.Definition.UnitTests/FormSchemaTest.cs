using System.ComponentModel;
using Bee.Definition.Forms;
using Bee.Base;
using Bee.Base.Data;

namespace Bee.Definition.UnitTests
{
    [Collection("Initialize")]
    public class FormSchemaTest
    {
        [Fact]
        [DisplayName("FormSchema 建立部門表單定義應包含有效的主檔表")]
        public void CreateFormSchema_DepartmentWithRelations_HasMasterTable()
        {
            var formSchema = new FormSchema("Department", "部門");
            var table = formSchema.Tables.Add("Department", "部門");
            table.DbTableName = "ft_department";
            table.Fields.Add("sys_no", "流水號", FieldDbType.AutoIncrement);
            table.Fields.Add("sys_rowid", "唯一識別", FieldDbType.Guid);
            table.Fields.Add("sys_id", "部門編號", FieldDbType.String);
            table.Fields.Add("sys_name", "部門名稱", FieldDbType.String);
            table.Fields.Add(
                new FormField("manager_rowid", "部門主管唯一識別", FieldDbType.String)
                {
                    RelationProgId = "Employee",
                    RelationFieldMappings =
                    {
                        { "sys_id", "ref_manager_id" },
                        { "sys_name", "ref_manager_name" }
                    }
                });
            table.Fields.Add(new FormField("ref_manager_id", "部門主管編號", FieldDbType.String, FieldType.RelationField));
            table.Fields.Add(new FormField("ref_manager_name", "部門主管名稱", FieldDbType.String, FieldType.RelationField));

            Assert.NotNull(formSchema.MasterTable);

            //string filePath = DefinePathInfo.GetFormSchemaFilePath(formSchema.ProgId);
            //formSchema.SetObjectFilePath(filePath);
            //formSchema.Save();
        }

        [Fact]
        [DisplayName("FormSchema 建立員工表單定義應包含關聯欄位參考")]
        public void CreateFormSchema_EmployeeWithRelations_HasRelationFieldReferences()
        {
            var formSchema = new FormSchema("Employee", "員工");
            var table = formSchema.Tables.Add("Employee", "員工");
            table.DbTableName = "ft_employee";
            table.Fields.Add("sys_no", "流水號", FieldDbType.AutoIncrement);
            table.Fields.Add("sys_rowid", "唯一識別", FieldDbType.Guid);
            table.Fields.Add("sys_id", "員工編號", FieldDbType.String);
            table.Fields.Add("sys_name", "員工姓名", FieldDbType.String);
            table.Fields.Add(
                new FormField("dept_rowid", "部門唯一識別", FieldDbType.String)
                {
                    RelationProgId = "Department",
                    RelationFieldMappings =
                    {
                        { "sys_id", "ref_dept_id" },
                        { "sys_name", "ref_dept_name" },
                        { "ref_manager_id", "ref_supervisor_id" },
                        { "ref_manager_name", "ref_supervisor_name" }
                    }
                });
            table.Fields.Add(new FormField("ref_dept_id", "部門編號", FieldDbType.String, FieldType.RelationField));
            table.Fields.Add(new FormField("ref_dept_name", "部門名稱", FieldDbType.String, FieldType.RelationField));
            table.Fields.Add(new FormField("ref_supervisor_id", "直屬主管編號", FieldDbType.String, FieldType.RelationField));
            table.Fields.Add(new FormField("ref_supervisor_name", "直屬主管名稱", FieldDbType.String, FieldType.RelationField));

            var references = table.RelationFieldReferences;

            Assert.NotNull(references);

            //string filePath = DefinePathInfo.GetFormSchemaFilePath(formSchema.ProgId);
            //formSchema.SetObjectFilePath(filePath);
            //formSchema.Save();
        }
    }
}
