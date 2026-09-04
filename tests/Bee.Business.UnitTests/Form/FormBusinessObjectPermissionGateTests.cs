using System.ComponentModel;
using System.Data;
using Bee.Base.Exceptions;
using Bee.Business.Form;
using Bee.Definition;
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
    /// FormBusinessObject 層一權限 gate 測試：以 fake ICompanyAuthorizationService 控制 Can 結果,
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
            var overrides = new List<(Type, object?)> { (typeof(ICompanyAuthorizationService), new FakeAuth(allowed)) };
            if (repo != null) { overrides.Add((typeof(IRepositoryFactory), new FakeFactory(repo))); }
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

        // 主表 Unchanged、只有表身（明細）被改 → 仍是對既存記錄的修改存檔（視為 Update）。
        private static DataSet DetailOnlyEditDataSet()
        {
            var ds = new DataSet();
            var master = ds.Tables.Add(GatedProgId);
            master.Columns.Add("sys_rowid", typeof(Guid));
            master.Columns.Add("sys_id");
            var mrow = master.NewRow();
            mrow["sys_rowid"] = Guid.NewGuid();
            mrow["sys_id"] = "m";
            master.Rows.Add(mrow);

            var detail = ds.Tables.Add(GatedProgId + "_Item");
            detail.Columns.Add("sys_rowid", typeof(Guid));
            detail.Columns.Add("qty");
            var drow = detail.NewRow();
            drow["sys_rowid"] = Guid.NewGuid();
            drow["qty"] = "1";
            detail.Rows.Add(drow);

            ds.AcceptChanges();   // 全部 Unchanged
            drow["qty"] = "2";    // 明細 → Modified；主表維持 Unchanged
            return ds;
        }

        /// <summary>
        /// 主檔表被整個省略、只送明細列——層二檢查在修正前會直接 early-return，
        /// 而 repository 照樣把明細寫進去。
        /// </summary>
        private static DataSet DetailRowsWithoutMasterTableDataSet()
        {
            var ds = new DataSet();
            var detail = ds.Tables.Add(GatedProgId + "_Item");
            detail.Columns.Add("sys_rowid", typeof(Guid));
            detail.Columns.Add(SysFields.MasterRowId, typeof(Guid));
            detail.Columns.Add("qty");
            var drow = detail.NewRow();
            drow["sys_rowid"] = Guid.NewGuid();
            drow[SysFields.MasterRowId] = Guid.NewGuid();   // 別人的主檔
            drow["qty"] = "1";
            detail.Rows.Add(drow);
            ds.AcceptChanges();
            drow["qty"] = "2";
            return ds;
        }

        /// <summary>
        /// 帶一個主檔，明細以 <paramref name="state"/> 的狀態指向另一筆<b>不在 payload 內</b>的主檔。
        /// </summary>
        /// <param name="state">明細列要呈現的狀態。</param>
        private static DataSet MasterWithDetailOwnedByAbsentMaster(DataRowState state)
        {
            var (ds, _, drow) = BuildMasterDetail();
            drow[SysFields.MasterRowId] = Guid.NewGuid();   // 別人的主檔

            if (state == DataRowState.Added) { return ds; }

            ds.AcceptChanges();
            drow["qty"] = "2";                             // → Modified，MasterRowId 兩版皆為別人的
            return ds;
        }

        /// <summary>
        /// 明細原本掛在別人的主檔下，被改嫁到本次 payload 帶著的主檔。
        /// </summary>
        /// <remarks>
        /// Current 指向在場的主檔，只有 Original 指向不在場的那一筆——<b>只驗 Current 的實作
        /// 抓不到這一種</b>，而它同樣是把別人記錄裡的資料搬走。這個案例存在的唯一理由就是
        /// 釘住 <c>WrittenVersions</c> 對 Modified 回傳兩個版本。
        /// </remarks>
        private static DataSet MasterWithDetailReparentedFromAbsentMaster()
        {
            var (ds, masterRowId, drow) = BuildMasterDetail();
            drow[SysFields.MasterRowId] = Guid.NewGuid();   // 別人的主檔
            ds.AcceptChanges();
            drow[SysFields.MasterRowId] = masterRowId;      // → Modified，改嫁到在場的主檔
            return ds;
        }

        /// <summary>
        /// 建一組「主檔一列 + 明細一列」，回傳 DataSet、主檔 rowid 與明細列（皆為 Added 狀態）。
        /// </summary>
        private static (DataSet DataSet, Guid MasterRowId, DataRow DetailRow) BuildMasterDetail()
        {
            var ds = new DataSet();
            var masterRowId = Guid.NewGuid();

            var master = ds.Tables.Add(GatedProgId);
            master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add("sys_id");
            var mrow = master.NewRow();
            mrow[SysFields.RowId] = masterRowId;
            mrow["sys_id"] = "m";
            master.Rows.Add(mrow);

            var detail = ds.Tables.Add(GatedProgId + "_Item");
            detail.Columns.Add(SysFields.RowId, typeof(Guid));
            detail.Columns.Add(SysFields.MasterRowId, typeof(Guid));
            detail.Columns.Add("qty");
            var drow = detail.NewRow();
            drow[SysFields.RowId] = Guid.NewGuid();
            drow["qty"] = "1";
            detail.Rows.Add(drow);

            return (ds, masterRowId, drow);
        }

        /// <summary>
        /// 正規的明細-only 編輯：主檔在場（Unchanged），明細指向的就是它。
        /// </summary>
        private static DataSet WellFormedDetailEditDataSet()
        {
            var ds = new DataSet();
            var masterRowId = Guid.NewGuid();

            var master = ds.Tables.Add(GatedProgId);
            master.Columns.Add(SysFields.RowId, typeof(Guid));
            master.Columns.Add("sys_id");
            var mrow = master.NewRow();
            mrow[SysFields.RowId] = masterRowId;
            mrow["sys_id"] = "m";
            master.Rows.Add(mrow);

            var detail = ds.Tables.Add(GatedProgId + "_Item");
            detail.Columns.Add(SysFields.RowId, typeof(Guid));
            detail.Columns.Add(SysFields.MasterRowId, typeof(Guid));
            detail.Columns.Add("qty");
            var drow = detail.NewRow();
            drow[SysFields.RowId] = Guid.NewGuid();
            drow[SysFields.MasterRowId] = masterRowId;
            drow["qty"] = "1";
            detail.Rows.Add(drow);

            ds.AcceptChanges();
            drow["qty"] = "2";
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
        [DisplayName("Save 只改表身（主表 Unchanged）視為 Update，越範圍應擋 ForbiddenException")]
        public void Save_DetailOnlyEdit_OutOfScope_ThrowsForbidden()
        {
            // 主表 Unchanged、明細 Modified → 仍是修改該既存記錄 → 走 Update 層二檢查
            var repo = new StubRepo { InScope = false };
            var bo = Bo(PermissionAction.Update, repo);
            Assert.Throws<ForbiddenException>(() => bo.Save(new SaveArgs { DataSet = DetailOnlyEditDataSet() }));
        }

        [Fact]
        [DisplayName("Save 省略主檔表、只送明細列應擋 ForbiddenException（層二繞過）")]
        public void Save_DetailRowsWithoutMasterTable_ThrowsForbidden()
        {
            // InScope=true：即使記錄範圍檢查會放行，這個 payload 形狀本身就沒有可檢查的主檔，
            // 所以擋下的理由是結構而非範圍——用 true 才證明得了這一點。
            var repo = new StubRepo { InScope = true };
            var bo = Bo(PermissionAction.Update, repo);

            Assert.Throws<ForbiddenException>(
                () => bo.Save(new SaveArgs { DataSet = DetailRowsWithoutMasterTableDataSet() }));
        }

        [Theory]
        [InlineData(DataRowState.Added)]
        [InlineData(DataRowState.Modified)]
        [DisplayName("Save 的明細指向 payload 未攜帶的主檔應擋 ForbiddenException")]
        public void Save_DetailOwnedByAbsentMaster_ThrowsForbidden(DataRowState state)
        {
            var repo = new StubRepo { InScope = true };
            var bo = Bo(PermissionAction.Create | PermissionAction.Update, repo);

            Assert.Throws<ForbiddenException>(
                () => bo.Save(new SaveArgs { DataSet = MasterWithDetailOwnedByAbsentMaster(state) }));
        }

        [Fact]
        [DisplayName("Save 把別人主檔下的明細改嫁到自己的主檔應擋 ForbiddenException（Original 版本）")]
        public void Save_DetailReparentedFromAbsentMaster_ThrowsForbidden()
        {
            var repo = new StubRepo { InScope = true };
            var bo = Bo(PermissionAction.Update, repo);

            Assert.Throws<ForbiddenException>(
                () => bo.Save(new SaveArgs { DataSet = MasterWithDetailReparentedFromAbsentMaster() }));
        }

        [Fact]
        [DisplayName("對照組：主檔在場且明細指向它的正規明細編輯應放行")]
        public void Save_WellFormedDetailEdit_PassesGate()
        {
            // 沒有這一條，上面三個測試用「一律拒絕」也能滿足。
            var repo = new StubRepo { InScope = true };
            var bo = Bo(PermissionAction.Update, repo);

            var ex = Record.Exception(() => bo.Save(new SaveArgs { DataSet = WellFormedDetailEditDataSet() }));

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

        private sealed class FakeAuth : ICompanyAuthorizationService
        {
            private readonly PermissionAction _allowed;
            public FakeAuth(PermissionAction allowed) { _allowed = allowed; }
            public bool Can(Guid accessToken, string modelId, PermissionAction action) => _allowed.HasFlag(action);
        }

        private sealed class FakeFactory : IRepositoryFactory
        {
            private readonly IDataFormRepository _repo;
            public FakeFactory(IDataFormRepository repo) { _repo = repo; }
            public T CreateFormRepository<T>(Guid accessToken, string progId) where T : class, IDataFormRepository => (T)_repo;
            public T Create<T>(Guid accessToken = default) where T : class => throw new NotSupportedException();
        }

        private sealed class StubRepo : IDataFormRepository
        {
            // Configurable authoritative in-scope verdict for write-scope tests; defaults to in-scope.
            public bool InScope { get; set; } = true;

            public DataFormListResult GetList(string selectFields, FilterNode? filter, SortFieldCollection? sortFields, PagingOptions? paging = null)
                => new() { Table = new DataTable() };
            public DataSet GetNewData(string timeZoneId = "") => new();
            public DataSet? GetData(Guid rowId, FilterNode? scopeFilter = null) => new();
            public (DataSet? Refreshed, Dictionary<string, int> AffectedRows) Save(DataSet dataSet) => (dataSet, new Dictionary<string, int>());
            public int Delete(Guid rowId, FilterNode? scopeFilter = null) => 1;
            public bool ExistsInScope(Guid rowId, FilterNode? scopeFilter) => InScope;
        }
    }
}
