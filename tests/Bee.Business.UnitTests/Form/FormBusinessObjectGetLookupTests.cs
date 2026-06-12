using System.ComponentModel;
using System.Data;
using Bee.Business.Form;
using Bee.Db;
using Bee.Db.Dml;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Repository.Form;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.Form
{
    /// <summary>
    /// Round-trip 整合測試：呼叫 <see cref="FormBusinessObject.GetLookup"/> 驗證
    /// server 端 lookup 欄位集解析（Employee 未宣告 LookupFields → 預設
    /// <c>sys_rowid,sys_id,sys_name</c>）、SearchText 過濾、預設分頁與
    /// <c>GetLookupFilter</c> 業務過濾覆寫點對實體 DB 的串接。
    /// </summary>
    public class FormBusinessObjectGetLookupTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        private const string CategoryId = "company";
        private const string ProgId = "Employee";

        public FormBusinessObjectGetLookupTests(SharedDbFixture fx) { _fx = fx; }

        [Fact]
        [DisplayName("GetLookup 傳入 null 應拋 ArgumentNullException")]
        public void GetLookup_NullArgs_Throws()
        {
            var bo = new FormBusinessObject(TestBeeContext.Create(_fx), Guid.NewGuid(), ProgId);
            Assert.Throws<ArgumentNullException>(() => bo.GetLookup(null!));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetLookup 預設投影應只含 sys_rowid/sys_id/sys_name 並套預設分頁")]
        public void GetLookup_Sqlite_DefaultProjectionAndPaging()
        {
            var ctx = new TestContext(_fx, DatabaseType.SQLite);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var emp1 = Guid.NewGuid();
            var emp2 = Guid.NewGuid();
            try
            {
                InsertEmployee(ctx, emp1, $"LK{runId}-1", "員工甲");
                InsertEmployee(ctx, emp2, $"LK{runId}-2", "員工乙");

                var result = ctx.CreateBo().GetLookup(new GetLookupArgs { SearchText = $"LK{runId}" });

                Assert.NotNull(result.Table);
                Assert.Equal(2, result.Table!.Rows.Count);
                // Server-resolved projection: exactly the default lookup field set.
                Assert.Equal(3, result.Table.Columns.Count);
                Assert.True(result.Table.Columns.Contains("sys_rowid"));
                Assert.True(result.Table.Columns.Contains("sys_id"));
                Assert.True(result.Table.Columns.Contains("sys_name"));
                // Omitted paging falls back to the server default page size.
                Assert.NotNull(result.Paging);
                Assert.Equal(100, result.Paging!.PageSize);
            }
            finally
            {
                TryDelete(ctx, emp1);
                TryDelete(ctx, emp2);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetLookup SearchText 應同時比對 sys_id 與 sys_name")]
        public void GetLookup_Sqlite_SearchTextMatchesIdOrName()
        {
            var ctx = new TestContext(_fx, DatabaseType.SQLite);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var emp1 = Guid.NewGuid();
            var emp2 = Guid.NewGuid();
            try
            {
                InsertEmployee(ctx, emp1, $"LK{runId}-1", $"甲{runId}");
                InsertEmployee(ctx, emp2, $"LK{runId}-2", "員工乙");

                // Matches emp1 by sys_name only — the id pattern is shared by both rows.
                var result = ctx.CreateBo().GetLookup(new GetLookupArgs { SearchText = $"甲{runId}" });

                Assert.NotNull(result.Table);
                Assert.Single(result.Table!.Rows);
                Assert.Equal($"LK{runId}-1", result.Table.Rows[0]["sys_id"]);
            }
            finally
            {
                TryDelete(ctx, emp1);
                TryDelete(ctx, emp2);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetLookupFilter 覆寫應 AND 縮小搜尋結果")]
        public void GetLookup_Sqlite_BusinessFilterNarrowsResult()
        {
            var ctx = new TestContext(_fx, DatabaseType.SQLite);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var emp1 = Guid.NewGuid();
            var emp2 = Guid.NewGuid();
            try
            {
                InsertEmployee(ctx, emp1, $"LK{runId}-1", "員工甲");
                InsertEmployee(ctx, emp2, $"LK{runId}-2", "員工乙");

                var bo = ctx.CreateFilteredBo(FilterCondition.Equal("sys_rowid", emp2));
                var result = bo.GetLookup(new GetLookupArgs { SearchText = $"LK{runId}" });

                Assert.NotNull(result.Table);
                Assert.Single(result.Table!.Rows);
                Assert.Equal($"LK{runId}-2", result.Table.Rows[0]["sys_id"]);
            }
            finally
            {
                TryDelete(ctx, emp1);
                TryDelete(ctx, emp2);
            }
        }

        /// <summary>
        /// 測試用 BO：以建構子注入的 FilterNode 作為 <see cref="FormBusinessObject.GetLookupFilter"/>
        /// 業務過濾，驗證 hook 與搜尋過濾的 AND 結合。
        /// </summary>
        private sealed class FilteredLookupBo : FormBusinessObject
        {
            private readonly FilterNode _filter;

            public FilteredLookupBo(IBeeContext ctx, Guid accessToken, string progId, FilterNode filter)
                : base(ctx, accessToken, progId)
            {
                _filter = filter;
            }

            protected override FilterNode? GetLookupFilter() => _filter;
        }

        private static void InsertEmployee(TestContext ctx, Guid rowId, string sysId, string sysName)
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
            row["dept_rowid"] = Guid.Empty;
            var spec = new InsertCommandBuilder(ctx.EmployeeSchema, ctx.DbType).Build("Employee", row);
            ctx.DbAccess.Execute(spec);
        }

        private static void TryDelete(TestContext ctx, Guid rowId)
        {
            try
            {
                var spec = new DeleteCommandBuilder(ctx.EmployeeSchema, ctx.DbType)
                    .Build("Employee", FilterCondition.Equal("sys_rowid", rowId));
                ctx.DbAccess.Execute(spec);
            }
            catch (Exception ex)
            {
                // 清理為 best-effort：種子 INSERT 失敗時可能對應列不存在；不要遮蔽斷言失敗訊息。
                Console.WriteLine($"FormBusinessObjectGetLookupTests: cleanup of Employee#{rowId} failed — {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Per-test wiring：將 <see cref="FormBusinessObject"/> 綁定到以測試專用
        /// <c>{categoryId}_{dbtype}</c> databaseId 建構的 <see cref="DataFormRepository"/>。
        /// </summary>
        private sealed class TestContext
        {
            private readonly SharedDbFixture _fx;
            private readonly IDataFormRepository _repository;

            public TestContext(SharedDbFixture fx, DatabaseType dbType)
            {
                _fx = fx;
                DbType = dbType;
                var databaseId = TestDbConventions.GetDatabaseId(dbType, CategoryId);
                DbAccess = fx.NewDbAccess(databaseId);

                var defineAccess = fx.GetRequiredService<IDefineAccess>();
                EmployeeSchema = defineAccess.GetFormSchema("Employee");

                _repository = new DataFormRepository(
                    ProgId,
                    EmployeeSchema,
                    defineAccess,
                    fx.GetRequiredService<IDbAccessFactory>(),
                    fx.GetRequiredService<IDbConnectionManager>(),
                    databaseId);
            }

            public DatabaseType DbType { get; }
            public DbAccess DbAccess { get; }
            public FormSchema EmployeeSchema { get; }

            public FormBusinessObject CreateBo()
            {
                var ctx = CreateContext();
                return new FormBusinessObject(ctx, Guid.NewGuid(), ProgId);
            }

            public FormBusinessObject CreateFilteredBo(FilterNode filter)
            {
                var ctx = CreateContext();
                return new FilteredLookupBo(ctx, Guid.NewGuid(), ProgId, filter);
            }

            private IBeeContext CreateContext()
            {
                var factory = new StubFactory(_repository);
                return TestBeeContext.CreateWithOverrides(_fx, (typeof(IFormRepositoryFactory), factory));
            }
        }

        private sealed class StubFactory : IFormRepositoryFactory
        {
            private readonly IDataFormRepository _repository;
            public StubFactory(IDataFormRepository repository) => _repository = repository;
            public IDataFormRepository CreateDataFormRepository(string progId, Guid accessToken) => _repository;
            public IReportFormRepository CreateReportFormRepository(string progId)
                => throw new NotSupportedException();
        }
    }
}
