using System.ComponentModel;
using Bee.Base.Data;
using Bee.Db.CacheNotify;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// cache-notify 的 poll 游標基準：讀取端的「現在」必須與寫入端戳 <c>sys_update_time</c>
    /// 用的是同一個基準。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 兩端曾經各有一份方言對照表：寫入端（欄位 DEFAULT 與 <c>CacheNotifyService</c> 的 upsert）
    /// 用 UTC，讀取端的空表 baseline 用**伺服器本地時間**（<c>getdate()</c> /
    /// <c>LOCALTIMESTAMP</c> / <c>CURRENT_TIMESTAMP(6)</c>）。
    /// </para>
    /// <para>
    /// 危害：資料庫伺服器若在 UTC 以東，全新部署（<c>st_cache_notify</c> 空表）的第一個 baseline
    /// 會落在未來，之後每次 poll 的視窗都撈不到任何列 —— **快取失效機制靜默停擺**，直到牆鐘時間追上。
    /// UTC+8 就是八小時。實測（2026-09-04）：PG 與 MySQL 在 UTC+8 session 下兩式相差正好 8 小時。
    /// </para>
    /// <para>
    /// <b>為什麼本機與 CI 都看不到</b>：本機容器與 GitHub runner 都跑 UTC，兩式在那裡剛好相等。
    /// 所以下面第一條測試**不查值、查表達式**——那是唯一在 UTC 環境下也成立的驗法。
    /// </para>
    /// </remarks>
    public class CacheNotifyBaselineBasisTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public CacheNotifyBaselineBasisTests(SharedDbFixture fx) { _fx = fx; }

        public static TheoryData<DatabaseType> SupportedDialects() =>
            [DatabaseType.SQLServer, DatabaseType.PostgreSQL, DatabaseType.MySQL,
             DatabaseType.Oracle, DatabaseType.SQLite];

        [Theory]
        [MemberData(nameof(SupportedDialects))]
        [DisplayName("空表 baseline 的時間表達式必須與寫入端戳 sys_update_time 的完全相同")]
        public void BaselineNowExpression_MatchesTheWriteSideExpression(DatabaseType databaseType)
        {
            // 寫入端：欄位 DEFAULT 與 CacheNotifyService 的 upsert 都讀這一個。
            string writeSide = DbDialectRegistry.Get(databaseType).GetDefaultValueExpression(FieldDbType.DateTime);
            Assert.NotEqual(string.Empty, writeSide);   // 防空轉：取不到值時下面的比對沒有意義

            string readSide = CacheNotifyReader.BaselineNowCommandText(databaseType);

            Assert.Contains(writeSide, readSide, StringComparison.Ordinal);
            Assert.StartsWith("SELECT ", readSide, StringComparison.Ordinal);
        }

        [DbTheory(DatabaseType.SQLServer)]
        [InlineData(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：baseline 語句實際執行的結果應貼近 UTC 而非伺服器本地時間")]
        public void BaselineNow_SqlServer_ReturnsUtc(DatabaseType databaseType) => AssertBaselineIsUtc(databaseType);

        [DbTheory(DatabaseType.PostgreSQL)]
        [InlineData(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：baseline 語句實際執行的結果應貼近 UTC 而非伺服器本地時間")]
        public void BaselineNow_PostgreSql_ReturnsUtc(DatabaseType databaseType) => AssertBaselineIsUtc(databaseType);

        [DbTheory(DatabaseType.MySQL)]
        [InlineData(DatabaseType.MySQL)]
        [DisplayName("MySQL：baseline 語句實際執行的結果應貼近 UTC 而非伺服器本地時間")]
        public void BaselineNow_MySql_ReturnsUtc(DatabaseType databaseType) => AssertBaselineIsUtc(databaseType);

        [DbTheory(DatabaseType.Oracle)]
        [InlineData(DatabaseType.Oracle)]
        [DisplayName("Oracle：baseline 語句實際執行的結果應貼近 UTC 而非伺服器本地時間")]
        public void BaselineNow_Oracle_ReturnsUtc(DatabaseType databaseType) => AssertBaselineIsUtc(databaseType);

        /// <summary>
        /// 實跑 baseline 語句，確認它回的是 UTC。
        /// </summary>
        /// <param name="databaseType">目標資料庫。</param>
        /// <remarks>
        /// 容器跑 UTC 時本條與本地時間無從區分，它擋的是「語句本身壞掉／方言不接受」——
        /// 表達式的基準由上面那條 Theory 負責。兩條互補。
        /// </remarks>
        private void AssertBaselineIsUtc(DatabaseType databaseType)
        {
            var dbAccess = _fx.NewDbAccess(TestDbConventions.GetDatabaseId(databaseType));
            string sql = CacheNotifyReader.BaselineNowCommandText(databaseType);

            var value = Convert.ToDateTime(dbAccess.ExecuteScalar(sql),
                System.Globalization.CultureInfo.InvariantCulture);

            // 寬鬆到分鐘級：這裡要抓的是「差了整個時區」，不是時鐘微小偏移。
            Assert.InRange(value, DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(10));
        }
    }
}
