using System.ComponentModel;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Logging;
using Bee.Hosting.Audit;
using Bee.Repository.AuditLog;
using Bee.Tests.Shared;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 讀取側整合測試（每方言各一）：透過寫入側 sink 把數筆 <c>st_log_change</c> 寫入 log 資料庫，
    /// 再以 <see cref="AuditLogRepository"/> 讀回，驗證參數化查詢、<c>log_time</c> DESC 排序、
    /// 以及 company 過濾。對應 <c>BEE_TEST_CONNSTR_*</c> 未設定時自動跳過。
    /// </summary>
    public class AuditLogQueryDbFactTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public AuditLogQueryDbFactTests(SharedDbFixture fx) { _fx = fx; }

        private void RunRecordHistoryQuery(DatabaseType databaseType)
        {
            var databaseId = TestDbConventions.GetDatabaseId(databaseType, "log");
            var connectionManager = _fx.GetRequiredService<IDbConnectionManager>();
            var dbAccess = new Bee.Db.DbAccess(databaseId, connectionManager);

            // Unique keys so the append-only log stays isolated from other rows / reruns / processes.
            const string progId = "Employee";
            var rowKey = Guid.NewGuid().ToString();

            // Two events for the same record in two companies, written oldest-first with distinct times.
            var older = new DateTime(2026, 7, 8, 1, 0, 0, DateTimeKind.Utc);
            var newer = new DateTime(2026, 7, 8, 2, 0, 0, DateTimeKind.Utc);
            dbAccess.Execute(AuditLogDbSink.BuildInsert(ChangeEntry(progId, rowKey, "c1", ChangeKind.Insert, older)));
            dbAccess.Execute(AuditLogDbSink.BuildInsert(ChangeEntry(progId, rowKey, "c2", ChangeKind.Update, newer)));

            var repository = new AuditLogRepository(connectionManager, databaseId);

            // No company filter → both rows, newest first. Compare wall-clock directly: the log
            // column is timezone-naive, so the round-trip loses DateTimeKind — asserting the DESC
            // ordering avoids a spurious local/UTC conversion.
            var all = repository.GetRecordChangeHistory(progId, rowKey, companyId: null);
            Assert.Equal(2, all.Rows.Count);
            var first = (DateTime)all.Rows[0]["log_time"];
            var second = (DateTime)all.Rows[1]["log_time"];
            Assert.True(first > second, $"Expected log_time DESC, got {first:o} then {second:o}.");

            // Company filter → only that company's row. company_id is a WHERE predicate, not a
            // selected column, so confirm the surviving row via change_kind (c1 = Insert, c2 = Update).
            var c1 = repository.GetRecordChangeHistory(progId, rowKey, companyId: "c1");
            Assert.Single(c1.Rows);
            Assert.Equal((int)ChangeKind.Insert, Convert.ToInt32(c1.Rows[0]["change_kind"], System.Globalization.CultureInfo.InvariantCulture));

            // A non-matching record key yields an empty table, not null.
            var none = repository.GetRecordChangeHistory(progId, Guid.NewGuid().ToString(), companyId: null);
            Assert.Empty(none.Rows);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：GetRecordChangeHistory 應依 log_time DESC 讀回並支援 company 過濾")]
        public void RecordHistory_SqlServer() => RunRecordHistoryQuery(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：GetRecordChangeHistory 應依 log_time DESC 讀回並支援 company 過濾")]
        public void RecordHistory_PostgreSQL() => RunRecordHistoryQuery(DatabaseType.PostgreSQL);

        private static ChangeAuditEntry ChangeEntry(string progId, string rowKey, string companyId, ChangeKind kind, DateTime logTimeUtc)
            => new ChangeAuditEntry
            {
                SysRowId = Guid.NewGuid(),
                LogTimeUtc = logTimeUtc,
                UserId = "demo",
                UserName = "Demo User",
                CompanyId = companyId,
                CompanyName = "Company " + companyId,
                ProgId = progId,
                ChangeTableName = "st_employee",
                RowKey = rowKey,
                ChangeKind = kind,
                IsSensitive = false,
                ChangesXml = "<diffgr:diffgram xmlns:diffgr=\"urn:schemas-microsoft-com:xml-diffgram-v1\" />",
            };
    }
}
