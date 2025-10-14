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
            table.Fields.Add("sys_no", "流水號", FieldDbType.Identity);
            table.Fields.Add("sys_rowid", "唯一識別", FieldDbType.Guid);
            table.Fields.Add("sys_id", "部門編號", FieldDbType.String);
            table.Fields.Add("sys_name", "部門名稱", FieldDbType.String);

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
            table.Fields.Add("sys_no", "流水號", FieldDbType.Identity);
            table.Fields.Add("sys_rowid", "唯一識別", FieldDbType.Guid);
            table.Fields.Add("sys_id", "員工編號", FieldDbType.String);
            table.Fields.Add("sys_name", "員工姓名", FieldDbType.String);
            table.Fields.Add(new FormField("dept_id", "部門編號", FieldDbType.String)
            {
                RelationProgId = "Department",
                RelationFieldMappings = { { "sys_name", "ref_dept_name" } }
            });
            table.Fields.Add(new FormField("ref_dept_name", "部門名稱", FieldDbType.String)
            {
                Type = FieldType.RelationField
            });

            var references = table.RelationFieldReferences;

            Assert.NotNull(references);

            //string filePath = DefinePathInfo.GetFormDefineFilePath(formDefine.ProgId);
            //formDefine.SetObjectFilePath(filePath);
            //formDefine.Save();
        }
    }
}
