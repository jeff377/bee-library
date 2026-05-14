using System.ComponentModel;
using System.Data;
using Bee.Business.Form;
using Bee.Db;
using Bee.Db.Dml;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Sorting;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Form;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.Form
{
    /// <summary>
    /// Round-trip 整合測試：呼叫 <see cref="FormBusinessObject.GetList"/> 驗證
    /// BO → <see cref="DataFormRepository"/> → <c>IFormCommandBuilder</c> → 實體 DB 的串接。
    /// 種子資料 + 清理沿用 <c>EmployeeBuildSelectIntegrationTests</c> 模式。
    /// </summary>
    public class FormBusinessObjectGetListTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        private const string CategoryId = "company";
        private const string ProgId = "Employee";

        public FormBusinessObjectGetListTests(SharedDbFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("GetList 傳入 null 應拋 ArgumentNullException")]
        public void GetList_NullArgs_Throws()
        {
            var bo = new FormBusinessObject(TestBeeContext.Create(_fx), Guid.NewGuid(), ProgId);
            Assert.Throws<ArgumentNullException>(() => bo.GetList(null!));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetList 指定明確 SelectFields 應只回傳該欄位且含關聯欄位")]
        public void GetList_Sqlite_ExplicitSelectFields()
            => RunExplicitSelectFields(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetList 同時套用 Filter 與 Sort 應回傳對應列數與順序")]
        public void GetList_Sqlite_FilterAndSort()
            => RunFilterAndSort(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：GetList 指定明確 SelectFields 應只回傳該欄位且含關聯欄位")]
        public void GetList_SqlServer_ExplicitSelectFields()
            => RunExplicitSelectFields(DatabaseType.SQLServer);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：GetList 同時套用 Filter 與 Sort 應回傳對應列數與順序")]
        public void GetList_SqlServer_FilterAndSort()
            => RunFilterAndSort(DatabaseType.SQLServer);

        private void RunExplicitSelectFields(DatabaseType dbType)
        {
            var ctx = new TestContext(_fx, dbType);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var deptRowId = Guid.NewGuid();
            var employeeRowId = Guid.NewGuid();

            try
            {
                InsertDepartment(ctx, deptRowId, $"D{runId}", "工程部", Guid.Empty);
                InsertEmployee(ctx, employeeRowId, $"E{runId}", "員工乙", deptRowId);

                var bo = ctx.CreateBo();
                var args = new GetListArgs
                {
                    SelectFields = "sys_id,sys_name,ref_dept_name",
                    Filter = FilterCondition.Equal("sys_rowid", employeeRowId)
                };
                var result = bo.GetList(args);

                Assert.NotNull(result.Table);
                Assert.Single(result.Table!.Rows);
                var row = result.Table.Rows[0];
                Assert.Equal($"E{runId}", row["sys_id"]);
                Assert.Equal("員工乙", row["sys_name"]);
                Assert.Equal("工程部", row["ref_dept_name"]);
                // 未要求的欄位不應存在
                Assert.False(result.Table.Columns.Contains("ref_supervisor_name"));
            }
            finally
            {
                TryDelete(ctx, "Employee", employeeRowId);
                TryDelete(ctx, "Department", deptRowId);
            }
        }

        private void RunFilterAndSort(DatabaseType dbType)
        {
            var ctx = new TestContext(_fx, dbType);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var deptZRowId = Guid.NewGuid();
            var deptARowId = Guid.NewGuid();
            var empInZRowId = Guid.NewGuid();
            var empInARowId = Guid.NewGuid();

            try
            {
                InsertDepartment(ctx, deptZRowId, $"DZ{runId}", "ZZZ", Guid.Empty);
                InsertDepartment(ctx, deptARowId, $"DA{runId}", "AAA", Guid.Empty);
                InsertEmployee(ctx, empInZRowId, $"EZ{runId}", "員工Z", deptZRowId);
                InsertEmployee(ctx, empInARowId, $"EA{runId}", "員工A", deptARowId);

                var bo = ctx.CreateBo();
                var args = new GetListArgs
                {
                    SelectFields = "sys_id,ref_dept_name",
                    Filter = FilterGroup.Any(
                        FilterCondition.Equal("sys_rowid", empInZRowId),
                        FilterCondition.Equal("sys_rowid", empInARowId)),
                    SortFields = [new SortField("ref_dept_name", SortDirection.Asc)]
                };
                var result = bo.GetList(args);

                Assert.NotNull(result.Table);
                Assert.Equal(2, result.Table!.Rows.Count);
                Assert.Equal("AAA", result.Table.Rows[0]["ref_dept_name"]);
                Assert.Equal($"EA{runId}", result.Table.Rows[0]["sys_id"]);
                Assert.Equal("ZZZ", result.Table.Rows[1]["ref_dept_name"]);
                Assert.Equal($"EZ{runId}", result.Table.Rows[1]["sys_id"]);
            }
            finally
            {
                TryDelete(ctx, "Employee", empInZRowId);
                TryDelete(ctx, "Employee", empInARowId);
                TryDelete(ctx, "Department", deptZRowId);
                TryDelete(ctx, "Department", deptARowId);
            }
        }

        private static void InsertEmployee(TestContext ctx, Guid rowId, string sysId, string sysName, Guid deptRowId)
        {
            var dt = new DataTable();
            dt.Columns.Add("sys_rowid", typeof(Guid));
            dt.Columns.Add("sys_id", typeof(string));
            dt.Columns.Add("sys_name", typeof(string));
            dt.Columns.Add("dept_rowid", typeof(Guid));
            var row = dt.NewRow();
            row["sys_rowid"] = rowId;
            row["sys_id"] = sysId;
            row["sys_name"] = sysName;
            row["dept_rowid"] = deptRowId;
            var spec = new InsertCommandBuilder(ctx.EmployeeSchema, ctx.DbType).Build("Employee", row);
            ctx.DbAccess.Execute(spec);
        }

        private static void InsertDepartment(TestContext ctx, Guid rowId, string sysId, string sysName, Guid managerRowId)
        {
            var dt = new DataTable();
            dt.Columns.Add("sys_rowid", typeof(Guid));
            dt.Columns.Add("sys_id", typeof(string));
            dt.Columns.Add("sys_name", typeof(string));
            dt.Columns.Add("manager_rowid", typeof(Guid));
            var row = dt.NewRow();
            row["sys_rowid"] = rowId;
            row["sys_id"] = sysId;
            row["sys_name"] = sysName;
            row["manager_rowid"] = managerRowId;
            var spec = new InsertCommandBuilder(ctx.DepartmentSchema, ctx.DbType).Build("Department", row);
            ctx.DbAccess.Execute(spec);
        }

        private static void TryDelete(TestContext ctx, string tableName, Guid rowId)
        {
            try
            {
                var schema = tableName == "Employee" ? ctx.EmployeeSchema : ctx.DepartmentSchema;
                var spec = new DeleteCommandBuilder(schema, ctx.DbType)
                    .Build(tableName, FilterCondition.Equal("sys_rowid", rowId));
                ctx.DbAccess.Execute(spec);
            }
            catch (Exception ex)
            {
                // 清理為 best-effort：種子 INSERT 失敗時可能對應列不存在；不要遮蔽斷言失敗訊息。
                Console.WriteLine($"FormBusinessObjectGetListTests: cleanup of {tableName}#{rowId} failed — {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Per-test wiring: binds <see cref="FormBusinessObject"/> to a <see cref="DataFormRepository"/>
        /// constructed against the test-specific <c>{categoryId}_{dbtype}</c> databaseId.
        /// The production <see cref="FormRepositoryFactory"/> uses <c>CategoryId</c> directly,
        /// which doesn't match the multi-DB-per-category test layout.
        /// </summary>
        private sealed class TestContext
        {
            private readonly SharedDbFixture _fx;
            private readonly string _databaseId;
            private readonly IDataFormRepository _repository;

            public TestContext(SharedDbFixture fx, DatabaseType dbType)
            {
                _fx = fx;
                DbType = dbType;
                _databaseId = TestDbConventions.GetDatabaseId(dbType, CategoryId);
                DbAccess = fx.NewDbAccess(_databaseId);

                var defineAccess = fx.GetRequiredService<IDefineAccess>();
                EmployeeSchema = defineAccess.GetFormSchema("Employee");
                DepartmentSchema = defineAccess.GetFormSchema("Department");

                _repository = new DataFormRepository(
                    ProgId,
                    EmployeeSchema,
                    defineAccess,
                    fx.GetRequiredService<IDbAccessFactory>(),
                    fx.GetRequiredService<IDbConnectionManager>(),
                    _databaseId);
            }

            public DatabaseType DbType { get; }
            public DbAccess DbAccess { get; }
            public FormSchema EmployeeSchema { get; }
            public FormSchema DepartmentSchema { get; }

            public FormBusinessObject CreateBo()
            {
                var factory = new StubFactory(_repository);
                var ctx = TestBeeContext.CreateWithOverrides(_fx, (typeof(IFormRepositoryFactory), factory));
                return new FormBusinessObject(ctx, Guid.NewGuid(), ProgId);
            }
        }

        private sealed class StubFactory : IFormRepositoryFactory
        {
            private readonly IDataFormRepository _repository;
            public StubFactory(IDataFormRepository repository) => _repository = repository;
            public IDataFormRepository CreateDataFormRepository(string progId) => _repository;
            public IReportFormRepository CreateReportFormRepository(string progId)
                => throw new NotSupportedException();
        }
    }
}
