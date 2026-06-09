using System.ComponentModel;
using Bee.Db.Providers.SqlServer;
using Bee.Definition.Filters;
using Bee.Definition.Sorting;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 以 tests/Define/FormSchema/Employee.FormSchema.xml 為對象，
    /// 驗證 <see cref="SqlFormCommandBuilder"/> 由 FormSchema 驅動產生 SELECT 語句的行為。
    /// 不需資料庫連線；純粹比對產出 SQL 字串。
    /// </summary>
    public class EmployeeBuildSelectTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public EmployeeBuildSelectTests(BeeTestFixture fx) { _fx = fx; }

        private IDefineAccess DefineAccess => _fx.GetRequiredService<IDefineAccess>();

        private SqlFormCommandBuilder NewBuilder()
            => new(DefineAccess.GetFormSchema("Employee"), DefineAccess);

        private static int CountJoins(string sql)
        {
            int count = 0;
            int index = 0;
            while ((index = sql.IndexOf("JOIN", index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                index += "JOIN".Length;
            }
            return count;
        }

        [Fact]
        [DisplayName("Employee BuildSelect 不指定欄位應產生含 st_employee 的 SELECT/FROM")]
        public void BuildSelect_AllFields_ContainsTableNameAndKeywords()
        {
            var builder = NewBuilder();

            var spec = builder.BuildSelect("Employee", string.Empty, null, null);

            Assert.NotNull(spec);
            Assert.Equal(DbCommandKind.DataTable, spec.Kind);
            Assert.Contains("SELECT", spec.CommandText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("FROM", spec.CommandText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("st_employee", spec.CommandText);
        }

        [Fact]
        [DisplayName("Employee BuildSelect 僅取主檔欄位時不應產生 JOIN")]
        public void BuildSelect_MasterFieldsOnly_NoJoin()
        {
            var builder = NewBuilder();

            var spec = builder.BuildSelect("Employee", "sys_id,sys_name", null, null);

            Assert.NotNull(spec);
            Assert.DoesNotContain("JOIN", spec.CommandText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("sys_id", spec.CommandText);
            Assert.Contains("sys_name", spec.CommandText);
        }

        [Fact]
        [DisplayName("Employee BuildSelect 取部門參考欄位應 JOIN 至 st_department")]
        public void BuildSelect_WithDeptRelationField_JoinsDepartment()
        {
            var builder = NewBuilder();

            // ref_dept_name 由 dept_rowid → Department.sys_name 對映取得
            var spec = builder.BuildSelect("Employee", "sys_id,sys_name,ref_dept_name", null, null);

            Assert.NotNull(spec);
            Assert.Contains("JOIN", spec.CommandText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("st_department", spec.CommandText);
            Assert.Equal(1, CountJoins(spec.CommandText));
        }

        [Fact]
        [DisplayName("Employee BuildSelect 取主管參考欄位應產生多層 JOIN（部門再連回員工）")]
        public void BuildSelect_WithSupervisorRelationField_GeneratesChainedJoins()
        {
            var builder = NewBuilder();

            // ref_supervisor_name 經由 dept_rowid → Department → manager_rowid → Employee 取得
            var spec = builder.BuildSelect("Employee", "sys_id,sys_name,ref_supervisor_name", null, null);

            Assert.NotNull(spec);
            int joins = CountJoins(spec.CommandText);
            Assert.True(joins >= 2, $"預期至少 2 個 JOIN（Department + Employee），實際 {joins}");
            Assert.Contains("st_department", spec.CommandText);
            Assert.Contains("st_employee", spec.CommandText);
        }

        [Fact]
        [DisplayName("Employee BuildSelect 篩選主檔欄位時不應產生 JOIN")]
        public void BuildSelect_FilterOnMasterField_NoJoinAndOneParameter()
        {
            var builder = NewBuilder();
            var filter = FilterCondition.Equal("sys_id", "E001");

            var spec = builder.BuildSelect("Employee", "sys_id,sys_name", filter, null);

            Assert.NotNull(spec);
            Assert.DoesNotContain("JOIN", spec.CommandText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("WHERE", spec.CommandText, StringComparison.OrdinalIgnoreCase);
            Assert.Single(spec.Parameters);
        }

        [Fact]
        [DisplayName("Employee BuildSelect 以參考欄位作為篩選條件時應產生 JOIN")]
        public void BuildSelect_FilterOnRelationField_GeneratesJoin()
        {
            var builder = NewBuilder();
            var filter = FilterCondition.Equal("ref_dept_id", "D001");

            var spec = builder.BuildSelect("Employee", "sys_id,sys_name", filter, null);

            Assert.NotNull(spec);
            Assert.Contains("JOIN", spec.CommandText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("st_department", spec.CommandText);
            Assert.Single(spec.Parameters);
        }

        [Fact]
        [DisplayName("Employee BuildSelect 以參考欄位排序時應產生 JOIN 與 ORDER BY")]
        public void BuildSelect_SortByRelationField_GeneratesJoinAndOrderBy()
        {
            var builder = NewBuilder();
            var sortFields = new SortFieldCollection
            {
                new SortField("ref_dept_name", SortDirection.Asc)
            };

            var spec = builder.BuildSelect("Employee", "sys_id,sys_name", null, sortFields);

            Assert.NotNull(spec);
            Assert.Contains("JOIN", spec.CommandText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("st_department", spec.CommandText);
            Assert.Contains("ORDER BY", spec.CommandText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        [DisplayName("Employee BuildSelect FilterGroup 多條件應產生對應參數量")]
        public void BuildSelect_FilterGroupWithMultipleConditions_ProducesParameters()
        {
            var builder = NewBuilder();

            var filterGroup = FilterGroup.All(
                FilterCondition.Contains("sys_name", "張"),
                FilterCondition.Equal("ref_dept_id", "D001")
            );

            var spec = builder.BuildSelect("Employee", "sys_id,sys_name", filterGroup, null);

            Assert.NotNull(spec);
            Assert.Equal(2, spec.Parameters.Count);
            Assert.Contains("JOIN", spec.CommandText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        [DisplayName("Employee BuildSelect 篩選主管參考欄位應產生多層 JOIN")]
        public void BuildSelect_FilterOnSupervisorRelationField_GeneratesChainedJoins()
        {
            var builder = NewBuilder();
            var filter = FilterCondition.StartsWith("ref_supervisor_name", "王");

            var spec = builder.BuildSelect("Employee", "sys_id,sys_name", filter, null);

            Assert.NotNull(spec);
            int joins = CountJoins(spec.CommandText);
            Assert.True(joins >= 2, $"預期至少 2 個 JOIN（Department + Employee），實際 {joins}");
            Assert.Single(spec.Parameters);
        }

        [Fact]
        [DisplayName("Employee BuildSelect 指定不存在的表名應擲 InvalidOperationException")]
        public void BuildSelect_UnknownTableName_Throws()
        {
            var builder = NewBuilder();

            Assert.Throws<InvalidOperationException>(
                () => builder.BuildSelect("DoesNotExist", string.Empty, null, null));
        }
    }
}
