using System.ComponentModel;
using System.Data;
using Bee.Base.Exceptions;
using Bee.Business.Form;
using Bee.Definition.Filters;
using Bee.Definition.Identity;
using Bee.Definition.Paging;
using Bee.Definition.Settings;
using Bee.Definition.Sorting;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.Form;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.Form
{
    /// <summary>
    /// FormBusinessObject 層一權限 gate 測試：以 fake IAuthorizationService 控制 Can 結果,
    /// 驗證越權 action 被擋（ForbiddenException）、有權放行、Save 逐列 RowState、空
    /// PermissionModelId 跳過。攔截路徑在進 repository 前即擋,故不需真實 DB。
    /// </summary>
    public class FormBusinessObjectPermissionGateTests : IClassFixture<SharedDbFixture>
    {
        // FormSchema 'PermGateForm' 宣告 PermissionModelId='PermGateModel' → gate 啟用。
        private const string GatedProgId = "PermGateForm";
        // 'Employee' 未宣告 PermissionModelId → gate 跳過。
        private const string UngatedProgId = "Employee";

        private readonly SharedDbFixture _fx;
        public FormBusinessObjectPermissionGateTests(SharedDbFixture fx) { _fx = fx; }

        private FormBusinessObject Bo(PermissionAction allowed, IDataFormRepository? repo = null, string progId = GatedProgId)
        {
            var overrides = new List<(Type, object?)> { (typeof(IAuthorizationService), new FakeAuth(allowed)) };
            if (repo != null) { overrides.Add((typeof(IFormRepositoryFactory), new FakeFactory(repo))); }
            var ctx = TestBeeContext.CreateWithOverrides(_fx, overrides.ToArray());
            return new FormBusinessObject(ctx, Guid.NewGuid(), progId);
        }

        private static DataSet AddedRowDataSet()
        {
            var ds = new DataSet();
            var table = ds.Tables.Add(GatedProgId);
            table.Columns.Add("sys_id");
            var row = table.NewRow();
            row["sys_id"] = "x";
            table.Rows.Add(row); // RowState = Added
            return ds;
        }

        private static DataSet ModifiedRowDataSet()
        {
            var ds = new DataSet();
            var table = ds.Tables.Add(GatedProgId);
            table.Columns.Add("sys_rowid", typeof(Guid));
            table.Columns.Add("sys_id");
            var row = table.NewRow();
            row["sys_rowid"] = Guid.NewGuid();
            row["sys_id"] = "x";
            table.Rows.Add(row);
            table.AcceptChanges();   // → Unchanged
            row["sys_id"] = "y";     // → Modified（觸發 Update 層二檢查）
            return ds;
        }

        [Fact]
        [DisplayName("GetList 無 Read 授權應擋 ForbiddenException")]
        public void GetList_NoReadGrant_ThrowsForbidden()
            => Assert.Throws<ForbiddenException>(() => Bo(PermissionAction.None).GetList(new GetListArgs()));

        [Fact]
        [DisplayName("GetData 無 Read 授權應擋 ForbiddenException")]
        public void GetData_NoReadGrant_ThrowsForbidden()
            => Assert.Throws<ForbiddenException>(() => Bo(PermissionAction.None).GetData(new GetDataArgs { RowId = Guid.NewGuid() }));

        [Fact]
        [DisplayName("Delete 無 Delete 授權應擋 ForbiddenException")]
        public void Delete_NoDeleteGrant_ThrowsForbidden()
            => Assert.Throws<ForbiddenException>(() => Bo(PermissionAction.None).Delete(new DeleteArgs { RowId = Guid.NewGuid() }));

        [Fact]
        [DisplayName("Save 含 Added 列但無 Create 授權應擋（逐列 RowState→Create）")]
        public void Save_AddedRow_NoCreateGrant_ThrowsForbidden()
        {
            // 持有 Update|Delete 但缺 Create → Added 列觸發的 Create 被擋
            var bo = Bo(PermissionAction.Update | PermissionAction.Delete);
            Assert.Throws<ForbiddenException>(() => bo.Save(new SaveArgs { DataSet = AddedRowDataSet() }));
        }

        [Fact]
        [DisplayName("GetList 有 Read 授權應放行進 repository")]
        public void GetList_WithReadGrant_PassesGate()
        {
            var bo = Bo(PermissionAction.Read, new StubRepo());

            var ex = Record.Exception(() => bo.GetList(new GetListArgs()));

            Assert.Null(ex); // gate 放行,repo 回 stub
        }

        [Fact]
        [DisplayName("Save 含 Added 列且有 Create 授權應放行")]
        public void Save_AddedRow_WithCreateGrant_PassesGate()
        {
            var bo = Bo(PermissionAction.Create, new StubRepo());

            var ex = Record.Exception(() => bo.Save(new SaveArgs { DataSet = AddedRowDataSet() }));

            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("Save 含 Modified 列但記錄越範圍（ExistsInScope=false）應擋 ForbiddenException（層二寫入）")]
        public void Save_ModifiedRow_OutOfScope_ThrowsForbidden()
        {
            // 有 Update 授權（層一過）但目標記錄不在範圍（權威 re-query=false）→ 層二擋
            var repo = new StubRepo { InScope = false };
            var bo = Bo(PermissionAction.Update, repo);
            Assert.Throws<ForbiddenException>(() => bo.Save(new SaveArgs { DataSet = ModifiedRowDataSet() }));
        }

        [Fact]
        [DisplayName("Save 含 Modified 列且記錄在範圍（ExistsInScope=true）應放行")]
        public void Save_ModifiedRow_InScope_PassesGate()
        {
            var repo = new StubRepo { InScope = true };
            var bo = Bo(PermissionAction.Update, repo);
            var ex = Record.Exception(() => bo.Save(new SaveArgs { DataSet = ModifiedRowDataSet() }));
            Assert.Null(ex);
        }

        [Fact]
        [DisplayName("FormSchema 未宣告 PermissionModelId 時 gate 應跳過（向後相容）")]
        public void EmptyPermissionModelId_SkipsGate()
        {
            // Employee 無 PermissionModelId → 即使 Can 全否,gate 也不查、直接放行
            var bo = Bo(PermissionAction.None, new StubRepo(), UngatedProgId);

            var ex = Record.Exception(() => bo.GetList(new GetListArgs()));

            Assert.Null(ex);
        }

        private sealed class FakeAuth : IAuthorizationService
        {
            private readonly PermissionAction _allowed;
            public FakeAuth(PermissionAction allowed) { _allowed = allowed; }
            public bool Can(Guid accessToken, string modelId, PermissionAction action) => _allowed.HasFlag(action);
        }

        private sealed class FakeFactory : IFormRepositoryFactory
        {
            private readonly IDataFormRepository _repo;
            public FakeFactory(IDataFormRepository repo) { _repo = repo; }
            public IDataFormRepository CreateDataFormRepository(string progId, Guid accessToken) => _repo;
            public IReportFormRepository CreateReportFormRepository(string progId) => throw new NotSupportedException();
        }

        private sealed class StubRepo : IDataFormRepository
        {
            // Configurable authoritative in-scope verdict for write-scope tests; defaults to in-scope.
            public bool InScope { get; set; } = true;

            public DataFormListResult GetList(string selectFields, FilterNode? filter, SortFieldCollection? sortFields, PagingOptions? paging = null)
                => new() { Table = new DataTable() };
            public DataSet GetNewData() => new();
            public DataSet? GetData(Guid rowId, FilterNode? scopeFilter = null) => new();
            public (DataSet? Refreshed, Dictionary<string, int> AffectedRows) Save(DataSet dataSet) => (dataSet, new Dictionary<string, int>());
            public int Delete(Guid rowId, FilterNode? scopeFilter = null) => 1;
            public bool ExistsInScope(Guid rowId, FilterNode? scopeFilter) => InScope;
        }
    }
}
