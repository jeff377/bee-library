using System.ComponentModel;
using System.Data;
using Bee.Business.Form;
using Bee.Db;
using Bee.Db.Dml;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Paging;
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

        // -------- Record-scope shaped filters (Phase 3) --------

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetList 套用 dept_rowid IN（scope 形狀）應只回該部門列且不報 remap 錯")]
        public void GetList_Sqlite_InFilterOnDeptField() => RunInFilterOnDeptField(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：GetList 套用 dept_rowid IN（scope 形狀）應只回該部門列且不報 remap 錯")]
        public void GetList_SqlServer_InFilterOnDeptField() => RunInFilterOnDeptField(DatabaseType.SQLServer);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetData 帶 scope filter — 範圍內回資料、越範圍回 null")]
        public void GetData_Sqlite_ScopeFilter() => RunGetDataScope(DatabaseType.SQLite);

        private void RunInFilterOnDeptField(DatabaseType dbType)
        {
            var ctx = new TestContext(_fx, dbType);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var deptA = Guid.NewGuid();
            var deptB = Guid.NewGuid();
            var empA = Guid.NewGuid();
            var empB = Guid.NewGuid();
            try
            {
                InsertDepartment(ctx, deptA, $"DA{runId}", "A部", Guid.Empty);
                InsertDepartment(ctx, deptB, $"DB{runId}", "B部", Guid.Empty);
                InsertEmployee(ctx, empA, $"EA{runId}", "員工A", deptA);
                InsertEmployee(ctx, empB, $"EB{runId}", "員工B", deptB);

                // scope 形狀：dept_rowid IN (deptA)。WhereBuilder 須把主表欄 dept_rowid 正確 remap。
                var result = ctx.CreateBo().GetList(new GetListArgs
                {
                    SelectFields = "sys_id,dept_rowid",
                    Filter = new FilterCondition
                    {
                        FieldName = "dept_rowid",
                        Operator = ComparisonOperator.In,
                        Value = new List<object> { deptA },
                    },
                });

                Assert.NotNull(result.Table);
                Assert.Single(result.Table!.Rows);  // 只有 A 部的 empA
                Assert.Equal($"EA{runId}", result.Table.Rows[0]["sys_id"]);
            }
            finally
            {
                TryDelete(ctx, "Employee", empA);
                TryDelete(ctx, "Employee", empB);
                TryDelete(ctx, "Department", deptA);
                TryDelete(ctx, "Department", deptB);
            }
        }

        private void RunGetDataScope(DatabaseType dbType)
        {
            var ctx = new TestContext(_fx, dbType);
            string runId = Guid.NewGuid().ToString("N")[..8];
            var deptA = Guid.NewGuid();
            var empA = Guid.NewGuid();
            try
            {
                InsertDepartment(ctx, deptA, $"DA{runId}", "A部", Guid.Empty);
                InsertEmployee(ctx, empA, $"EA{runId}", "員工A", deptA);

                var inScope = new FilterCondition { FieldName = "dept_rowid", Operator = ComparisonOperator.In, Value = new List<object> { deptA } };
                var outScope = new FilterCondition { FieldName = "dept_rowid", Operator = ComparisonOperator.In, Value = new List<object> { Guid.NewGuid() } };

                Assert.NotNull(ctx.Repository.GetData(empA, inScope));   // 範圍內
                Assert.Null(ctx.Repository.GetData(empA, outScope));     // 越範圍 → null
                Assert.NotNull(ctx.Repository.GetData(empA));            // 無 scope → 正常回
            }
            finally
            {
                TryDelete(ctx, "Employee", empA);
                TryDelete(ctx, "Department", deptA);
            }
        }

        // -------- Paging --------

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetList Paging=null 行為與既有不分頁路徑一致；Result.Paging=null")]
        public void GetList_Sqlite_PagingNull_NoPagingInfo()
            => RunPagingNullBehavior(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetList 分頁含 IncludeTotalCount 應回傳正確 TotalCount/HasMore")]
        public void GetList_Sqlite_PagedWithTotalCount()
            => RunPagedWithTotalCount(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetList 分頁不含 IncludeTotalCount 應 probe 推算 HasMore 且 TotalCount=null")]
        public void GetList_Sqlite_PagedWithoutTotalCount()
            => RunPagedWithoutTotalCount(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetList 分頁 Page 超過總頁數應回空 Table 且 HasMore=false")]
        public void GetList_Sqlite_PagedBeyondLastPage()
            => RunPagedBeyondLastPage(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetList PageSize 超過 MaxPageSize 應 clamp 至上限不丟例外")]
        public void GetList_Sqlite_PageSizeClampedToCap()
            => RunPageSizeClampedToCap(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：GetList 分頁 SortFields=null 應 fallback sys_no ASC")]
        public void GetList_Sqlite_SortFallbackToSysNo()
            => RunSortFallbackToSysNo(DatabaseType.SQLite);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：GetList 分頁含 IncludeTotalCount 應回傳正確 TotalCount/HasMore")]
        public void GetList_SqlServer_PagedWithTotalCount()
            => RunPagedWithTotalCount(DatabaseType.SQLServer);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：GetList 分頁不含 IncludeTotalCount 應 probe 推算 HasMore 且 TotalCount=null")]
        public void GetList_SqlServer_PagedWithoutTotalCount()
            => RunPagedWithoutTotalCount(DatabaseType.SQLServer);

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

        // Seeds 5 employees with sys_id "P{runId}-0" .. "P{runId}-4" and returns the
        // rowIds plus a StartsWith filter scoped to this run. Caller is responsible
        // for cleanup via TryDelete.
        private static (Guid[] rowIds, FilterNode filter, string runId) SeedFivePagingRows(TestContext ctx)
        {
            string runId = Guid.NewGuid().ToString("N")[..8];
            var rowIds = new Guid[5];
            for (int i = 0; i < 5; i++)
            {
                rowIds[i] = Guid.NewGuid();
                InsertEmployee(ctx, rowIds[i], $"P{runId}-{i}", $"員工{i}", Guid.Empty);
            }
            return (rowIds, FilterCondition.StartsWith("sys_id", $"P{runId}-"), runId);
        }

        private void RunPagingNullBehavior(DatabaseType dbType)
        {
            var ctx = new TestContext(_fx, dbType);
            var (rowIds, filter, _) = SeedFivePagingRows(ctx);
            try
            {
                var result = ctx.CreateBo().GetList(new GetListArgs
                {
                    SelectFields = "sys_id",
                    Filter = filter,
                });

                Assert.NotNull(result.Table);
                Assert.Equal(5, result.Table!.Rows.Count);
                Assert.Null(result.Paging);  // 不分頁路徑：Paging 必為 null
            }
            finally
            {
                foreach (var id in rowIds) TryDelete(ctx, "Employee", id);
            }
        }

        private void RunPagedWithTotalCount(DatabaseType dbType)
        {
            var ctx = new TestContext(_fx, dbType);
            var (rowIds, filter, runId) = SeedFivePagingRows(ctx);
            try
            {
                var result = ctx.CreateBo().GetList(new GetListArgs
                {
                    SelectFields = "sys_id",
                    Filter = filter,
                    SortFields = [new SortField("sys_id", SortDirection.Asc)],
                    Paging = new PagingOptions { Page = 2, PageSize = 2, IncludeTotalCount = true },
                });

                Assert.NotNull(result.Table);
                Assert.Equal(2, result.Table!.Rows.Count);
                Assert.Equal($"P{runId}-2", result.Table.Rows[0]["sys_id"]);
                Assert.Equal($"P{runId}-3", result.Table.Rows[1]["sys_id"]);

                Assert.NotNull(result.Paging);
                Assert.Equal(2, result.Paging!.Page);
                Assert.Equal(2, result.Paging.PageSize);
                Assert.Equal(5, result.Paging.TotalCount);
                Assert.True(result.Paging.HasMore);  // 5 列，第 2 頁取 2 列、後面還有 1 列
            }
            finally
            {
                foreach (var id in rowIds) TryDelete(ctx, "Employee", id);
            }
        }

        private void RunPagedWithoutTotalCount(DatabaseType dbType)
        {
            var ctx = new TestContext(_fx, dbType);
            var (rowIds, filter, runId) = SeedFivePagingRows(ctx);
            try
            {
                var result = ctx.CreateBo().GetList(new GetListArgs
                {
                    SelectFields = "sys_id",
                    Filter = filter,
                    SortFields = [new SortField("sys_id", SortDirection.Asc)],
                    Paging = new PagingOptions { Page = 1, PageSize = 2, IncludeTotalCount = false },
                });

                Assert.NotNull(result.Table);
                // probe row 已 trim：頁面只留 2 列
                Assert.Equal(2, result.Table!.Rows.Count);
                Assert.Equal($"P{runId}-0", result.Table.Rows[0]["sys_id"]);
                Assert.Equal($"P{runId}-1", result.Table.Rows[1]["sys_id"]);

                Assert.NotNull(result.Paging);
                Assert.Null(result.Paging!.TotalCount);  // 未要求
                Assert.True(result.Paging.HasMore);
            }
            finally
            {
                foreach (var id in rowIds) TryDelete(ctx, "Employee", id);
            }
        }

        private void RunPagedBeyondLastPage(DatabaseType dbType)
        {
            var ctx = new TestContext(_fx, dbType);
            var (rowIds, filter, _) = SeedFivePagingRows(ctx);
            try
            {
                var result = ctx.CreateBo().GetList(new GetListArgs
                {
                    SelectFields = "sys_id",
                    Filter = filter,
                    SortFields = [new SortField("sys_id", SortDirection.Asc)],
                    Paging = new PagingOptions { Page = 99, PageSize = 2, IncludeTotalCount = true },
                });

                Assert.NotNull(result.Table);
                Assert.Empty(result.Table!.Rows);

                Assert.NotNull(result.Paging);
                Assert.Equal(5, result.Paging!.TotalCount);
                Assert.False(result.Paging.HasMore);
            }
            finally
            {
                foreach (var id in rowIds) TryDelete(ctx, "Employee", id);
            }
        }

        private void RunPageSizeClampedToCap(DatabaseType dbType)
        {
            var ctx = new TestContext(_fx, dbType);
            var (rowIds, filter, _) = SeedFivePagingRows(ctx);
            try
            {
                var result = ctx.CreateBo().GetList(new GetListArgs
                {
                    SelectFields = "sys_id",
                    Filter = filter,
                    SortFields = [new SortField("sys_id", SortDirection.Asc)],
                    // int.MaxValue should be clamped to MaxPageSize (1000) without exceptions.
                    Paging = new PagingOptions { Page = 1, PageSize = int.MaxValue, IncludeTotalCount = false },
                });

                Assert.NotNull(result.Table);
                Assert.Equal(5, result.Table!.Rows.Count);  // 5 列、cap 後 PageSize 仍夠裝下
                Assert.NotNull(result.Paging);
                Assert.Equal(1000, result.Paging!.PageSize);  // clamp 結果
                Assert.False(result.Paging.HasMore);
            }
            finally
            {
                foreach (var id in rowIds) TryDelete(ctx, "Employee", id);
            }
        }

        private void RunSortFallbackToSysNo(DatabaseType dbType)
        {
            var ctx = new TestContext(_fx, dbType);
            var (rowIds, filter, runId) = SeedFivePagingRows(ctx);
            try
            {
                // SortFields = null → Repository fallback 套用 sys_no ASC。
                // sys_no 為 AutoIncrement，種子順序遞增；分頁第 1 頁取 2 列、第 2 頁再取 2 列，
                // sys_id 也跟著保持 0..4 順序（因為兩個欄位插入順序一致）。
                var page1 = ctx.CreateBo().GetList(new GetListArgs
                {
                    SelectFields = "sys_id",
                    Filter = filter,
                    SortFields = null,
                    Paging = new PagingOptions { Page = 1, PageSize = 2, IncludeTotalCount = false },
                });

                Assert.Equal(2, page1.Table!.Rows.Count);
                Assert.Equal($"P{runId}-0", page1.Table.Rows[0]["sys_id"]);
                Assert.Equal($"P{runId}-1", page1.Table.Rows[1]["sys_id"]);

                var page2 = ctx.CreateBo().GetList(new GetListArgs
                {
                    SelectFields = "sys_id",
                    Filter = filter,
                    SortFields = null,
                    Paging = new PagingOptions { Page = 2, PageSize = 2, IncludeTotalCount = false },
                });

                Assert.Equal(2, page2.Table!.Rows.Count);
                Assert.Equal($"P{runId}-2", page2.Table.Rows[0]["sys_id"]);
                Assert.Equal($"P{runId}-3", page2.Table.Rows[1]["sys_id"]);
            }
            finally
            {
                foreach (var id in rowIds) TryDelete(ctx, "Employee", id);
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
            public IDataFormRepository Repository => _repository;

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
            public IDataFormRepository CreateDataFormRepository(string progId, Guid accessToken) => _repository;
            public IReportFormRepository CreateReportFormRepository(string progId)
                => throw new NotSupportedException();
        }
    }
}
