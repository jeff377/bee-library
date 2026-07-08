using System.ComponentModel;
using System.Data;
using System.Globalization;
using Bee.Business.AuditLog;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.AuditLog;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests.AuditLog
{
    /// <summary>
    /// <see cref="LogBusinessObject.GetRecordHistory"/> 的 BO 層行為測試：以 stub repository 回傳
    /// 已知 <c>st_log_change</c> 列（含手工 DiffGram），驗證 DiffGram 還原為結構化 before/after、
    /// 權限 gate（未授權擋下）、參數驗證，以及查詢引數透傳。不接實體 DB。
    /// </summary>
    public class LogBusinessObjectGetRecordHistoryTests : IClassFixture<BeeTestFixture>
    {
        private const string ProgId = "Employee";
        private readonly BeeTestFixture _fx;

        public LogBusinessObjectGetRecordHistoryTests(BeeTestFixture fx) { _fx = fx; }

        private LogBusinessObject Bo(IAuditLogRepository repo, bool authorized = true)
        {
            var ctx = TestBeeContext.CreateWithOverrides(_fx,
                (typeof(IAuthorizationService), new FakeAuth(authorized)),
                (typeof(IAuditLogRepositoryFactory), new StubAuditLogRepositoryFactory(repo)));
            return new LogBusinessObject(ctx, Guid.NewGuid());
        }

        [Fact]
        [DisplayName("GetRecordHistory 應把 changes_xml DiffGram 還原為欄位級 before/after")]
        public void GetRecordHistory_Authorized_RestoresModifiedFields()
        {
            var rowKey = Guid.NewGuid().ToString();
            var logRowId = Guid.NewGuid();
            var xml = BuildModifyDiffGram("st_employee", rowKey, "name", "Alice", "Alice Wang");
            var repo = new StubAuditLogRepository(ChangeTable((logRowId, ChangeKind.Update, "demo", xml)));

            var result = Bo(repo).GetRecordHistory(new GetRecordHistoryArgs { ProgId = ProgId, RowKey = rowKey });

            Assert.Equal(ProgId, result.ProgId);
            Assert.Equal(rowKey, result.RowKey);
            var entry = Assert.Single(result.Changes);
            Assert.Equal(logRowId, entry.SysRowId);
            Assert.Equal(ChangeKind.Update, entry.ChangeKind);
            Assert.Equal("demo", entry.UserId);
            var field = Assert.Single(entry.Fields);
            Assert.Equal("st_employee", field.TableName);
            Assert.Equal(rowKey, field.RowKey);
            Assert.Equal("name", field.FieldName);
            Assert.Equal("Alice", field.OldValue);
            Assert.Equal("Alice Wang", field.NewValue);
            Assert.Equal(ChangeKind.Update, field.RowState);
        }

        [Fact]
        [DisplayName("GetRecordHistory 未授權應丟 UnauthorizedAccessException")]
        public void GetRecordHistory_NotAuthorized_Throws()
        {
            var repo = new StubAuditLogRepository(ChangeTable());
            var bo = Bo(repo, authorized: false);

            Assert.Throws<UnauthorizedAccessException>(() =>
                bo.GetRecordHistory(new GetRecordHistoryArgs { ProgId = ProgId, RowKey = Guid.NewGuid().ToString() }));
        }

        [Theory]
        [InlineData("", "r1")]
        [InlineData("Employee", "")]
        [DisplayName("GetRecordHistory 缺 ProgId 或 RowKey 應丟 ArgumentException")]
        public void GetRecordHistory_MissingKey_Throws(string progId, string rowKey)
        {
            var bo = Bo(new StubAuditLogRepository(ChangeTable()));

            Assert.Throws<ArgumentException>(() =>
                bo.GetRecordHistory(new GetRecordHistoryArgs { ProgId = progId, RowKey = rowKey }));
        }

        [Fact]
        [DisplayName("GetRecordHistory 應把 ProgId / RowKey 透傳給 repository")]
        public void GetRecordHistory_PassesQueryKeysToRepository()
        {
            var rowKey = Guid.NewGuid().ToString();
            var repo = new StubAuditLogRepository(ChangeTable());
            var bo = Bo(repo);

            bo.GetRecordHistory(new GetRecordHistoryArgs { ProgId = ProgId, RowKey = rowKey });

            Assert.Equal(ProgId, repo.LastProgId);
            Assert.Equal(rowKey, repo.LastRowKey);
        }

        [Fact]
        [DisplayName("GetRecordHistory 多筆事件應全部映射且順序不變")]
        public void GetRecordHistory_MultipleEvents_MappedInOrder()
        {
            var rowKey = Guid.NewGuid().ToString();
            var insertXml = BuildInsertDiffGram("st_employee", rowKey, "name", "Alice");
            var deleteXml = BuildDeleteDiffGram("st_employee", rowKey, "name", "Alice Wang");
            var repo = new StubAuditLogRepository(ChangeTable(
                (Guid.NewGuid(), ChangeKind.Delete, "demo", deleteXml),
                (Guid.NewGuid(), ChangeKind.Insert, "demo", insertXml)));

            var result = Bo(repo).GetRecordHistory(new GetRecordHistoryArgs { ProgId = ProgId, RowKey = rowKey });

            Assert.Equal(2, result.Changes.Count);
            Assert.Equal(ChangeKind.Delete, result.Changes[0].ChangeKind);
            Assert.Equal(ChangeKind.Insert, result.Changes[1].ChangeKind);
            // Delete carries a before-image (old value, null new); insert carries the new value only.
            Assert.Equal("Alice Wang", Assert.Single(result.Changes[0].Fields).OldValue);
            Assert.Null(Assert.Single(result.Changes[0].Fields).NewValue);
            Assert.Equal("Alice", Assert.Single(result.Changes[1].Fields).NewValue);
            Assert.Null(Assert.Single(result.Changes[1].Fields).OldValue);
        }

        private static DataTable ChangeTable(params (Guid rowId, ChangeKind kind, string userId, string xml)[] rows)
        {
            var table = new DataTable("st_log_change");
            table.Columns.Add("sys_rowid", typeof(Guid));
            table.Columns.Add("log_time", typeof(DateTime));
            table.Columns.Add("user_id", typeof(string));
            table.Columns.Add("user_name", typeof(string));
            table.Columns.Add("change_kind", typeof(int));
            table.Columns.Add("is_sensitive", typeof(bool));
            table.Columns.Add("source", typeof(string));
            table.Columns.Add("changes_xml", typeof(string));
            var logTime = new DateTime(2026, 7, 8, 3, 0, 0, DateTimeKind.Utc);
            foreach (var r in rows)
            {
                table.Rows.Add(r.rowId, logTime, r.userId, "User " + r.userId, (int)r.kind, false,
                    ProgId + ".Save", r.xml);
            }
            return table;
        }

        private static string BuildModifyDiffGram(string tableName, string rowKey, string column, string oldValue, string newValue)
        {
            var ds = new DataSet("Root");
            var table = ds.Tables.Add(tableName);
            table.Columns.Add("sys_rowid", typeof(string));
            table.Columns.Add(column, typeof(string));
            var row = table.Rows.Add(rowKey, oldValue);
            ds.AcceptChanges();
            row[column] = newValue;
            return DiffGram(ds);
        }

        private static string BuildInsertDiffGram(string tableName, string rowKey, string column, string value)
        {
            var ds = new DataSet("Root");
            var table = ds.Tables.Add(tableName);
            table.Columns.Add("sys_rowid", typeof(string));
            table.Columns.Add(column, typeof(string));
            table.Rows.Add(rowKey, value);
            return DiffGram(ds);
        }

        private static string BuildDeleteDiffGram(string tableName, string rowKey, string column, string value)
        {
            var ds = new DataSet("Root");
            var table = ds.Tables.Add(tableName);
            table.Columns.Add("sys_rowid", typeof(string));
            table.Columns.Add(column, typeof(string));
            var row = table.Rows.Add(rowKey, value);
            ds.AcceptChanges();
            row.Delete();
            return DiffGram(ds);
        }

        private static string DiffGram(DataSet ds)
        {
            using var changes = ds.GetChanges()!;
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            changes.WriteXml(writer, XmlWriteMode.DiffGram);
            return writer.ToString();
        }

        private sealed class FakeAuth : IAuthorizationService
        {
            private readonly bool _allowed;
            public FakeAuth(bool allowed) { _allowed = allowed; }
            public bool Can(Guid accessToken, string modelId, PermissionAction action) => _allowed;
        }

        private sealed class StubAuditLogRepository : IAuditLogRepository
        {
            private readonly DataTable _table;
            public string? LastProgId { get; private set; }
            public string? LastRowKey { get; private set; }
            public string? LastCompanyId { get; private set; }

            public StubAuditLogRepository(DataTable table) { _table = table; }

            public DataTable GetRecordChangeHistory(string progId, string rowKey, string? companyId)
            {
                LastProgId = progId;
                LastRowKey = rowKey;
                LastCompanyId = companyId;
                return _table;
            }
        }

        private sealed class StubAuditLogRepositoryFactory : IAuditLogRepositoryFactory
        {
            private readonly IAuditLogRepository _repo;
            public StubAuditLogRepositoryFactory(IAuditLogRepository repo) { _repo = repo; }
            public IAuditLogRepository CreateAuditLogRepository() => _repo;
        }
    }
}
