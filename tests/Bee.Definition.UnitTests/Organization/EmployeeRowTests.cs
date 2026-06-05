using System.ComponentModel;
using Bee.Definition.Organization;

namespace Bee.Definition.UnitTests.Organization
{
    public class EmployeeRowTests
    {
        [Fact]
        [DisplayName("EmployeeRow ctor 應正確設定所有屬性")]
        public void Ctor_SetsAllProperties()
        {
            var rowId = Guid.NewGuid();
            var deptRowId = Guid.NewGuid();
            var userRowId = Guid.NewGuid();

            var row = new EmployeeRow(rowId, "EMP001", "張三", deptRowId, userRowId);

            Assert.Equal(rowId, row.RowId);
            Assert.Equal("EMP001", row.EmployeeId);
            Assert.Equal("張三", row.EmployeeName);
            Assert.Equal(deptRowId, row.DeptRowId);
            Assert.Equal(userRowId, row.UserRowId);
        }

        [Fact]
        [DisplayName("EmployeeRow 相同屬性值的兩個實例應相等")]
        public void Equality_SameValues_AreEqual()
        {
            var rowId = Guid.NewGuid();
            var deptRowId = Guid.NewGuid();
            var userRowId = Guid.NewGuid();

            var a = new EmployeeRow(rowId, "EMP001", "張三", deptRowId, userRowId);
            var b = new EmployeeRow(rowId, "EMP001", "張三", deptRowId, userRowId);

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        [DisplayName("EmployeeRow ToString 應回傳含所有屬性資訊的字串")]
        public void ToString_ContainsPropertyValues()
        {
            var rowId = Guid.NewGuid();
            var row = new EmployeeRow(rowId, "EMP001", "張三", Guid.Empty, Guid.Empty);

            var str = row.ToString();

            Assert.Contains("EMP001", str);
            Assert.Contains("張三", str);
        }

        [Fact]
        [DisplayName("EmployeeRow Deconstruct 應正確拆解所有屬性")]
        public void Deconstruct_ReturnsAllProperties()
        {
            var rowId = Guid.NewGuid();
            var deptRowId = Guid.NewGuid();
            var userRowId = Guid.NewGuid();
            var row = new EmployeeRow(rowId, "EMP001", "張三", deptRowId, userRowId);

            var (actualRowId, actualEmpId, actualEmpName, actualDeptRowId, actualUserRowId) = row;

            Assert.Equal(rowId, actualRowId);
            Assert.Equal("EMP001", actualEmpId);
            Assert.Equal("張三", actualEmpName);
            Assert.Equal(deptRowId, actualDeptRowId);
            Assert.Equal(userRowId, actualUserRowId);
        }
    }
}
