using System.ComponentModel;
using System.Globalization;
using Bee.Db;
using Bee.Definition.Database;
using Bee.Definition.Logging;
using Bee.Hosting.Audit;
using Bee.Tests.Shared;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 端到端整合測試：透過 sink 產生的實際 INSERT 把一筆登入記錄寫入 log 資料庫的
    /// <c>st_log_login</c>，再讀回驗證。跑真實資料庫（每方言各一），對應
    /// <c>BEE_TEST_CONNSTR_*</c> 未設定時自動跳過。
    /// </summary>
    public class AuditLogDbFactTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public AuditLogDbFactTests(SharedDbFixture fx) { _fx = fx; }

        private void RunLoginRoundTrip(DatabaseType databaseType)
        {
            var databaseId = TestDbConventions.GetDatabaseId(databaseType, "log");
            var factory = _fx.GetRequiredService<IDbAccessFactory>();
            var dbAccess = factory.Create(databaseId);

            var rowId = Guid.NewGuid();
            var entry = new LoginAuditEntry
            {
                SysRowId = rowId,
                UserId = "demo",
                UserName = "Demo User",
                AccessToken = Guid.NewGuid(),
                Event = LoginEvent.LoginSucceeded,
            };

            // Executes the exact INSERT the sink produces, against the test log database.
            dbAccess.Execute(AuditLogDbSink.BuildInsert(entry));

            var result = dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_log_login WHERE sys_rowid={0}", rowId));
            var count = Convert.ToInt64(result.Scalar, CultureInfo.InvariantCulture);

            Assert.Equal(1L, count);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：登入記錄寫入 st_log_login 後可讀回")]
        public void LoginLog_SqlServer_RoundTrip() => RunLoginRoundTrip(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：登入記錄寫入 st_log_login 後可讀回")]
        public void LoginLog_PostgreSQL_RoundTrip() => RunLoginRoundTrip(DatabaseType.PostgreSQL);
    }
}
