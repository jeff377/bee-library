using Bee.Base;

namespace Bee.Define.UnitTests
{
    [Collection("Initialize")]
    public class FormDefineTest
    {
        [Fact]
        public void DepartmentFormTable()
        {
            var formDefine = new FormDefine("Department", "部門");
            var table = formDefine.Tables.Add("Department", "部門");
            table.DbTableName = "ft_department";
            table.Fields.Add("sys_no", "流水號", FieldDbType.Identity);
            table.Fields.Add("sys_rowid", "唯一識別", FieldDbType.Guid);
            table.Fields.Add("sys_id", "部門編號", FieldDbType.String);
            table.Fields.Add("sys_name", "部門名稱", FieldDbType.String);
            table.Fields.Add(
                new FormField("manager_id", "部門主管編號", FieldDbType.String)
                {
                    RelationProgId = "Employee",
                    RelationFieldMappings =
                    {
                        { "sys_name", "ref_manager_name" }
                    }
                });
            table.Fields.Add(new FormField("ref_manager_name", "部門主管名稱", FieldDbType.String, FieldType.RelationField));

            Assert.NotNull(formDefine.MasterTable);

            //string filePath = DefinePathInfo.GetFormDefineFilePath(formDefine.ProgId);
            //formDefine.SetObjectFilePath(filePath);
            //formDefine.Save();
        }

        [Fact]
        public void EmployeeFormTable()
        {
            var formDefine = new FormDefine("Employee", "員工");
            var table = formDefine.Tables.Add("Employee", "員工");
            table.DbTableName = "ft_employee";
            table.Fields.Add("sys_no", "流水號", FieldDbType.Identity);
            table.Fields.Add("sys_rowid", "唯一識別", FieldDbType.Guid);
            table.Fields.Add("sys_id", "員工編號", FieldDbType.String);
            table.Fields.Add("sys_name", "員工姓名", FieldDbType.String);
            table.Fields.Add(
                new FormField("dept_id", "部門編號", FieldDbType.String)
                {
                    RelationProgId = "Department",
                    RelationFieldMappings =
                    {
                        { "sys_name", "ref_dept_name" },
                        { "manager_id", "ref_supervisor_id" },
                        { "ref_manager_name", "ref_supervisor_name" }
                    }
                });
            table.Fields.Add(new FormField("ref_dept_name", "部門名稱", FieldDbType.String, FieldType.RelationField));
            table.Fields.Add(new FormField("ref_supervisor_id", "直屬主管編號", FieldDbType.String, FieldType.RelationField));
            table.Fields.Add(new FormField("ref_supervisor_name", "直屬主管名稱", FieldDbType.String, FieldType.RelationField));

            var references = table.RelationFieldReferences;

            Assert.NotNull(references);

            //string filePath = DefinePathInfo.GetFormDefineFilePath(formDefine.ProgId);
            //formDefine.SetObjectFilePath(filePath);
            //formDefine.Save();
        }
    }
}
