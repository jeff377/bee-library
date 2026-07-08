using System.ComponentModel;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Logging;
using Bee.Definition.Paging;
using Bee.Hosting.Audit;
using Bee.Repository.AuditLog;
using Bee.Repository.Abstractions.AuditLog;
using Bee.Tests.Shared;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 讀取側整合測試（每方言各一）：透過寫入側 sink 把數筆 <c>st_log_change</c> 寫入 log 資料庫，
    /// 再以 <see cref="AuditLogRepository"/> 讀回，驗證 <c>GetChangeLog</c> 的參數化 filter、
    /// <c>log_time</c> DESC 排序、company 過濾、分頁（TotalCount / HasMore），以及
    /// <c>GetChangeById</c> 單筆取值與 company scope。對應 <c>BEE_TEST_CONNSTR_*</c> 未設定時自動跳過。
    /// </summary>
    public class AuditLogQueryDbFactTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public AuditLogQueryDbFactTests(SharedDbFixture fx) { _fx = fx; }

        private void RunChangeLogQuery(DatabaseType databaseType)
        {
            var databaseId = TestDbConventions.GetDatabaseId(databaseType, "log");
            var connectionManager = _fx.GetRequiredService<IDbConnectionManager>();
            var dbAccess = new Bee.Db.DbAccess(databaseId, connectionManager);

            // Unique keys so the append-only log stays isolated from other rows / reruns / processes.
            const string progId = "Employee";
            var rowKey = Guid.NewGuid().ToString();
            var older = new DateTime(2026, 7, 8, 1, 0, 0, DateTimeKind.Utc);
            var newer = new DateTime(2026, 7, 8, 2, 0, 0, DateTimeKind.Utc);
            var olderId = Guid.NewGuid();
            var newerId = Guid.NewGuid();

            dbAccess.Execute(AuditLogDbSink.BuildInsert(ChangeEntry(olderId, progId, rowKey, "c1", ChangeKind.Insert, older)));
            dbAccess.Execute(AuditLogDbSink.BuildInsert(ChangeEntry(newerId, progId, rowKey, "c2", ChangeKind.Update, newer)));

            var repository = new AuditLogRepository(connectionManager, databaseId);
            var byRecord = new ChangeLogQuery { ProgId = progId, RowKey = rowKey };

            // No company filter → both rows, newest first (log_time DESC).
            var all = repository.GetChangeLog(byRecord, new PagingOptions { PageSize = 50 });
            Assert.Equal(2, all.Table.Rows.Count);
            Assert.Equal(newerId, (Guid)all.Table.Rows[0]["sys_rowid"]);
            Assert.Equal(olderId, (Guid)all.Table.Rows[1]["sys_rowid"]);
            // Header projection excludes the heavy DiffGram payload.
            Assert.False(all.Table.Columns.Contains("changes_xml"));

            // Company filter → only that company's row.
            var c1 = repository.GetChangeLog(
                new ChangeLogQuery { ProgId = progId, RowKey = rowKey, CompanyId = "c1" },
                new PagingOptions { PageSize = 50 });
            Assert.Single(c1.Table.Rows);
            Assert.Equal(olderId, (Guid)c1.Table.Rows[0]["sys_rowid"]);

            // Paging: PageSize 1 with total count → 1 row, TotalCount 2, HasMore true.
            var firstPage = repository.GetChangeLog(byRecord, new PagingOptions { Page = 1, PageSize = 1, IncludeTotalCount = true });
            Assert.Single(firstPage.Table.Rows);
            Assert.Equal(newerId, (Guid)firstPage.Table.Rows[0]["sys_rowid"]);
            Assert.Equal(2, firstPage.Paging.TotalCount);
            Assert.True(firstPage.Paging.HasMore);

            // Second page → the older row, no more.
            var secondPage = repository.GetChangeLog(byRecord, new PagingOptions { Page = 2, PageSize = 1, IncludeTotalCount = true });
            Assert.Single(secondPage.Table.Rows);
            Assert.Equal(olderId, (Guid)secondPage.Table.Rows[0]["sys_rowid"]);
            Assert.False(secondPage.Paging.HasMore);

            // GetChangeById → the row incl. changes_xml; out-of-company scope → null.
            var detail = repository.GetChangeById(olderId, companyId: "c1");
            Assert.NotNull(detail);
            Assert.Single(detail!.Rows);
            Assert.True(detail.Columns.Contains("changes_xml"));
            Assert.Null(repository.GetChangeById(olderId, companyId: "c2"));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：GetChangeLog 排序/過濾/分頁 + GetChangeById 應正確")]
        public void ChangeLog_SqlServer() => RunChangeLogQuery(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：GetChangeLog 排序/過濾/分頁 + GetChangeById 應正確")]
        public void ChangeLog_PostgreSQL() => RunChangeLogQuery(DatabaseType.PostgreSQL);

        private static ChangeAuditEntry ChangeEntry(Guid sysRowId, string progId, string rowKey, string companyId, ChangeKind kind, DateTime logTimeUtc)
            => new ChangeAuditEntry
            {
                SysRowId = sysRowId,
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
