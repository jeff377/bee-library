using System.ComponentModel;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <see cref="SessionRepository"/> 的種子讀寫測試，五家 provider 各跑一輪。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>st_session</c> 存的是重建種子而非 SessionInfo 快照——只放無法再推導的值
    /// （token / 使用者 / 到期 / 公司）。因此測試重點在 round-trip 與三個寫入操作的效果。
    /// </para>
    /// <para>
    /// 本類別的測試以 <see cref="ProviderScopedRouter"/> 把 <c>DbScope.Common</c> 導向該
    /// provider 的測試資料庫。用預設路由的話全部會落在 SQL Server：<c>UpdateSession</c> 的
    /// 佔位符順序是 <c>{1} {2} {0}</c>，而 Oracle 的位置綁定會把 <c>DateTime</c> 送進
    /// <c>access_token</c>（RAW(16)）——那個缺陷正是在這條路徑上，卻由壓測而非測試發現。
    /// </para>
    /// </remarks>
    public class SessionRepositoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public SessionRepositoryTests(SharedDbFixture fx) { _fx = fx; }

        private SessionRepository CreateRepo(DatabaseType databaseType)
            => new SessionRepository(
                TestRepositoryContext.Create(
                    _fx.GetRequiredService<IDbConnectionManager>(),
                    router: new ProviderScopedRouter(databaseType)),
                Guid.Empty,
                string.Empty);

        private static SessionUser CreateSeed(int expiresInSeconds = 3600, string? companyId = null)
            => new SessionUser
            {
                AccessToken = Guid.NewGuid(),
                UserID = "001",
                UserName = "測試管理員",
                EndTime = DateTime.UtcNow.AddSeconds(expiresInSeconds),
                CompanyId = companyId,
            };

        #region InsertSession + GetSession round-trip

        private void RunInsertThenGet(DatabaseType databaseType)
        {
            var repo = CreateRepo(databaseType);
            var seed = CreateSeed(companyId: "C001");

            repo.InsertSession(seed);

            var actual = repo.GetSession(seed.AccessToken);
            Assert.NotNull(actual);
            Assert.Equal(seed.AccessToken, actual!.AccessToken);
            Assert.Equal("001", actual.UserID);
            Assert.Equal("測試管理員", actual.UserName);
            Assert.Equal("C001", actual.CompanyId);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("InsertSession 寫入的種子應可由 GetSession 完整取回（SQL Server）")]
        public void InsertSession_ThenGetSession_RoundTrips_SqlServer() => RunInsertThenGet(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("InsertSession 寫入的種子應可由 GetSession 完整取回（PostgreSQL）")]
        public void InsertSession_ThenGetSession_RoundTrips_PostgreSql() => RunInsertThenGet(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("InsertSession 寫入的種子應可由 GetSession 完整取回（SQLite）")]
        public void InsertSession_ThenGetSession_RoundTrips_Sqlite() => RunInsertThenGet(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("InsertSession 寫入的種子應可由 GetSession 完整取回（MySQL）")]
        public void InsertSession_ThenGetSession_RoundTrips_MySql() => RunInsertThenGet(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("InsertSession 寫入的種子應可由 GetSession 完整取回（Oracle）")]
        public void InsertSession_ThenGetSession_RoundTrips_Oracle() => RunInsertThenGet(DatabaseType.Oracle);

        #endregion

        #region GetSession — 不存在的 token

        private void RunGetSessionNotFound(DatabaseType databaseType)
        {
            Assert.Null(CreateRepo(databaseType).GetSession(Guid.NewGuid()));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetSession 傳入不存在的 AccessToken 應回傳 null（SQL Server）")]
        public void GetSession_NonExistentToken_ReturnsNull_SqlServer() => RunGetSessionNotFound(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetSession 傳入不存在的 AccessToken 應回傳 null（PostgreSQL）")]
        public void GetSession_NonExistentToken_ReturnsNull_PostgreSql() => RunGetSessionNotFound(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("GetSession 傳入不存在的 AccessToken 應回傳 null（SQLite）")]
        public void GetSession_NonExistentToken_ReturnsNull_Sqlite() => RunGetSessionNotFound(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("GetSession 傳入不存在的 AccessToken 應回傳 null（MySQL）")]
        public void GetSession_NonExistentToken_ReturnsNull_MySql() => RunGetSessionNotFound(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("GetSession 傳入不存在的 AccessToken 應回傳 null（Oracle）")]
        public void GetSession_NonExistentToken_ReturnsNull_Oracle() => RunGetSessionNotFound(DatabaseType.Oracle);

        #endregion

        #region GetSession — 已過期的種子

        private void RunGetSessionExpired(DatabaseType databaseType)
        {
            var repo = CreateRepo(databaseType);
            var seed = CreateSeed(expiresInSeconds: -3600);
            repo.InsertSession(seed);

            Assert.Null(repo.GetSession(seed.AccessToken));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetSession 已過期的種子應回傳 null（SQL Server）")]
        public void GetSession_ExpiredSeed_ReturnsNull_SqlServer() => RunGetSessionExpired(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetSession 已過期的種子應回傳 null（PostgreSQL）")]
        public void GetSession_ExpiredSeed_ReturnsNull_PostgreSql() => RunGetSessionExpired(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("GetSession 已過期的種子應回傳 null（SQLite）")]
        public void GetSession_ExpiredSeed_ReturnsNull_Sqlite() => RunGetSessionExpired(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("GetSession 已過期的種子應回傳 null（MySQL）")]
        public void GetSession_ExpiredSeed_ReturnsNull_MySql() => RunGetSessionExpired(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("GetSession 已過期的種子應回傳 null（Oracle）")]
        public void GetSession_ExpiredSeed_ReturnsNull_Oracle() => RunGetSessionExpired(DatabaseType.Oracle);

        #endregion

        #region UpdateSession — 佔位符順序為 {1} {2} {0}，位置綁定會在此錯位

        private void RunUpdateSession(DatabaseType databaseType)
        {
            var repo = CreateRepo(databaseType);
            var seed = CreateSeed();
            repo.InsertSession(seed);

            seed.CompanyId = "C002";
            repo.UpdateSession(seed);

            Assert.Equal("C002", repo.GetSession(seed.AccessToken)!.CompanyId);

            // 離開公司即清空，重建才不會把使用者放回已離開的公司
            seed.CompanyId = null;
            repo.UpdateSession(seed);

            Assert.Null(repo.GetSession(seed.AccessToken)!.CompanyId);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("UpdateSession 應覆寫既有種子的 CompanyId（SQL Server）")]
        public void UpdateSession_OverwritesCompanyId_SqlServer() => RunUpdateSession(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("UpdateSession 應覆寫既有種子的 CompanyId（PostgreSQL）")]
        public void UpdateSession_OverwritesCompanyId_PostgreSql() => RunUpdateSession(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("UpdateSession 應覆寫既有種子的 CompanyId（SQLite）")]
        public void UpdateSession_OverwritesCompanyId_Sqlite() => RunUpdateSession(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("UpdateSession 應覆寫既有種子的 CompanyId（MySQL）")]
        public void UpdateSession_OverwritesCompanyId_MySql() => RunUpdateSession(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("UpdateSession 應覆寫既有種子的 CompanyId（Oracle）")]
        public void UpdateSession_OverwritesCompanyId_Oracle() => RunUpdateSession(DatabaseType.Oracle);

        #endregion

        #region DeleteSession

        private void RunDeleteSession(DatabaseType databaseType)
        {
            var repo = CreateRepo(databaseType);
            var seed = CreateSeed();
            repo.InsertSession(seed);

            repo.DeleteSession(seed.AccessToken);
            Assert.Null(repo.GetSession(seed.AccessToken));

            var exception = Record.Exception(() => repo.DeleteSession(seed.AccessToken));
            Assert.Null(exception);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("DeleteSession 應刪除種子且重複呼叫為冪等（SQL Server）")]
        public void DeleteSession_RemovesSeed_AndIsIdempotent_SqlServer() => RunDeleteSession(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("DeleteSession 應刪除種子且重複呼叫為冪等（PostgreSQL）")]
        public void DeleteSession_RemovesSeed_AndIsIdempotent_PostgreSql() => RunDeleteSession(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("DeleteSession 應刪除種子且重複呼叫為冪等（SQLite）")]
        public void DeleteSession_RemovesSeed_AndIsIdempotent_Sqlite() => RunDeleteSession(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("DeleteSession 應刪除種子且重複呼叫為冪等（MySQL）")]
        public void DeleteSession_RemovesSeed_AndIsIdempotent_MySql() => RunDeleteSession(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("DeleteSession 應刪除種子且重複呼叫為冪等（Oracle）")]
        public void DeleteSession_RemovesSeed_AndIsIdempotent_Oracle() => RunDeleteSession(DatabaseType.Oracle);

        #endregion

        #region GetSession 無副作用 + DeleteExpiredSessions

        private void RunGetSessionHasNoSideEffect(DatabaseType databaseType)
        {
            var repo = CreateRepo(databaseType);
            var expired = CreateSeed(expiresInSeconds: -3600);
            repo.InsertSession(expired);

            // 過期列由查詢條件過濾，不再 delete-on-read
            Assert.Null(repo.GetSession(expired.AccessToken));
            // 讀完該列仍在，交由清理排程回收——若讀取仍會刪除，這裡就沒有東西可刪了
            Assert.True(repo.DeleteExpiredSessions() >= 1);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetSession 不得產生任何寫入（讀取純化，SQL Server）")]
        public void GetSession_HasNoSideEffect_SqlServer() => RunGetSessionHasNoSideEffect(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetSession 不得產生任何寫入（讀取純化，PostgreSQL）")]
        public void GetSession_HasNoSideEffect_PostgreSql() => RunGetSessionHasNoSideEffect(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("GetSession 不得產生任何寫入（讀取純化，SQLite）")]
        public void GetSession_HasNoSideEffect_Sqlite() => RunGetSessionHasNoSideEffect(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("GetSession 不得產生任何寫入（讀取純化，MySQL）")]
        public void GetSession_HasNoSideEffect_MySql() => RunGetSessionHasNoSideEffect(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("GetSession 不得產生任何寫入（讀取純化，Oracle）")]
        public void GetSession_HasNoSideEffect_Oracle() => RunGetSessionHasNoSideEffect(DatabaseType.Oracle);

        #endregion

        #region DeleteExpiredSessions 只刪過期列

        private void RunDeleteExpiredSessions(DatabaseType databaseType)
        {
            var repo = CreateRepo(databaseType);
            var live = CreateSeed();
            var expired = CreateSeed(expiresInSeconds: -3600);
            repo.InsertSession(live);
            repo.InsertSession(expired);

            repo.DeleteExpiredSessions();

            Assert.NotNull(repo.GetSession(live.AccessToken));

            // 冪等：第二次執行不應再影響未過期列，也不應擲例外
            var exception = Record.Exception(() => repo.DeleteExpiredSessions());
            Assert.Null(exception);
            Assert.NotNull(repo.GetSession(live.AccessToken));

            repo.DeleteSession(live.AccessToken);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("DeleteExpiredSessions 應刪除過期列、保留未過期列且重複執行冪等（SQL Server）")]
        public void DeleteExpiredSessions_RemovesOnlyExpired_AndIsIdempotent_SqlServer() => RunDeleteExpiredSessions(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("DeleteExpiredSessions 應刪除過期列、保留未過期列且重複執行冪等（PostgreSQL）")]
        public void DeleteExpiredSessions_RemovesOnlyExpired_AndIsIdempotent_PostgreSql() => RunDeleteExpiredSessions(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("DeleteExpiredSessions 應刪除過期列、保留未過期列且重複執行冪等（SQLite）")]
        public void DeleteExpiredSessions_RemovesOnlyExpired_AndIsIdempotent_Sqlite() => RunDeleteExpiredSessions(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("DeleteExpiredSessions 應刪除過期列、保留未過期列且重複執行冪等（MySQL）")]
        public void DeleteExpiredSessions_RemovesOnlyExpired_AndIsIdempotent_MySql() => RunDeleteExpiredSessions(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("DeleteExpiredSessions 應刪除過期列、保留未過期列且重複執行冪等（Oracle）")]
        public void DeleteExpiredSessions_RemovesOnlyExpired_AndIsIdempotent_Oracle() => RunDeleteExpiredSessions(DatabaseType.Oracle);

        #endregion

        #region 未帶 CompanyId 的種子

        private void RunSeedWithoutCompanyId(DatabaseType databaseType)
        {
            var repo = CreateRepo(databaseType);
            var seed = CreateSeed();

            repo.InsertSession(seed);

            Assert.Null(repo.GetSession(seed.AccessToken)!.CompanyId);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("未帶 CompanyId 的種子應重建為未進公司狀態（SQL Server）")]
        public void GetSession_SeedWithoutCompanyId_RebuildsAsCompanyLess_SqlServer() => RunSeedWithoutCompanyId(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("未帶 CompanyId 的種子應重建為未進公司狀態（PostgreSQL）")]
        public void GetSession_SeedWithoutCompanyId_RebuildsAsCompanyLess_PostgreSql() => RunSeedWithoutCompanyId(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("未帶 CompanyId 的種子應重建為未進公司狀態（SQLite）")]
        public void GetSession_SeedWithoutCompanyId_RebuildsAsCompanyLess_Sqlite() => RunSeedWithoutCompanyId(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("未帶 CompanyId 的種子應重建為未進公司狀態（MySQL）")]
        public void GetSession_SeedWithoutCompanyId_RebuildsAsCompanyLess_MySql() => RunSeedWithoutCompanyId(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("未帶 CompanyId 的種子應重建為未進公司狀態（Oracle）")]
        public void GetSession_SeedWithoutCompanyId_RebuildsAsCompanyLess_Oracle() => RunSeedWithoutCompanyId(DatabaseType.Oracle);

        #endregion
    }
}
