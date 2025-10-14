using Bee.Base;

namespace Bee.Define.UnitTests
{
    public class FormDefineTest
    {
        [Fact]
        public void DepartmentFormTable()
        {
            var formDefine = new FormDefine()
            {
                ProgId = "Department",
                DisplayName = "部門"
            };
            var table = formDefine.Tables.Add("Department", "部門");
            table.Fields.Add("sys_no", "流水號", FieldDbType.Identity);
            table.Fields.Add("sys_rowid", "唯一識別", FieldDbType.Guid);
            table.Fields.Add("sys_id", "部門編號", FieldDbType.String);
            table.Fields.Add("sys_name", "部門名稱", FieldDbType.String);

            Assert.NotNull(table.Fields["sys_no"]);
            Assert.NotNull(table.Fields["sys_rowid"]);
            Assert.NotNull(table.Fields["sys_id"]);
            Assert.NotNull(table.Fields["sys_name"]);
            Assert.Equal(FieldDbType.Identity, table.Fields["sys_no"].DbType);
            Assert.Equal(FieldDbType.Guid, table.Fields["sys_rowid"].DbType);
        }

        [Fact]
        public void EmployeeFormTable()
        {
            var formDefine = new FormDefine()
            {
                ProgId = "Employee",
                DisplayName = "員工"
            };
            var table = formDefine.Tables.Add("Employee", "員工");
            table.Fields.Add("sys_no", "流水號", FieldDbType.Identity);
            table.Fields.Add("sys_rowid", "唯一識別", FieldDbType.Guid);
            table.Fields.Add("sys_id", "員工編號", FieldDbType.String);
            table.Fields.Add("sys_name", "員工姓名", FieldDbType.String);
            table.Fields.Add(new FormField("dept_id", "部門編號", FieldDbType.String)
            {
                RelationProgId = "Department",
                RelationFieldMappings = { { "sys_name", "dept_name" } }
            });
            table.Fields.Add(new FormField("dept_name", "部門名稱", FieldDbType.String)
            {
                Type = FieldType.RelationField
            });

            var references = table.RelationFieldReferences;

            Assert.NotNull(references);
        }
    }
}
