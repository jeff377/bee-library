using System.ComponentModel;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Identity;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <see cref="SessionRepository"/> 的種子讀寫測試。
    /// </summary>
    /// <remarks>
    /// <c>st_session</c> 存的是重建種子而非 SessionInfo 快照——只放無法再推導的值
    /// （token / 使用者 / 到期 / 公司）。因此測試重點在 round-trip 與三個寫入操作的效果。
    /// </remarks>
    public class SessionRepositoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public SessionRepositoryTests(SharedDbFixture fx) { _fx = fx; }

        private SessionRepository CreateRepo()
            => new SessionRepository(TestRepositoryContext.Create(_fx.GetRequiredService<IDbConnectionManager>()), Guid.Empty, string.Empty);

        private static SessionUser CreateSeed(int expiresInSeconds = 3600, string? companyId = null)
            => new SessionUser
            {
                AccessToken = Guid.NewGuid(),
                UserID = "001",
                UserName = "測試管理員",
                EndTime = DateTime.UtcNow.AddSeconds(expiresInSeconds),
                CompanyId = companyId,
            };

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("InsertSession 寫入的種子應可由 GetSession 完整取回")]
        public void InsertSession_ThenGetSession_RoundTrips()
        {
            var repo = CreateRepo();
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
        [DisplayName("GetSession 傳入不存在的 AccessToken 應回傳 null")]
        public void GetSession_NonExistentToken_ReturnsNull()
        {
            Assert.Null(CreateRepo().GetSession(Guid.NewGuid()));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetSession 已過期的種子應回傳 null")]
        public void GetSession_ExpiredSeed_ReturnsNull()
        {
            var repo = CreateRepo();
            var seed = CreateSeed(expiresInSeconds: -3600);
            repo.InsertSession(seed);

            Assert.Null(repo.GetSession(seed.AccessToken));
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("UpdateSession 應覆寫既有種子的 CompanyId")]
        public void UpdateSession_OverwritesCompanyId()
        {
            var repo = CreateRepo();
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
        [DisplayName("DeleteSession 應刪除種子且重複呼叫為冪等")]
        public void DeleteSession_RemovesSeed_AndIsIdempotent()
        {
            var repo = CreateRepo();
            var seed = CreateSeed();
            repo.InsertSession(seed);

            repo.DeleteSession(seed.AccessToken);
            Assert.Null(repo.GetSession(seed.AccessToken));

            var exception = Record.Exception(() => repo.DeleteSession(seed.AccessToken));
            Assert.Null(exception);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetSession 不得產生任何寫入（讀取純化）")]
        public void GetSession_HasNoSideEffect()
        {
            var repo = CreateRepo();
            var expired = CreateSeed(expiresInSeconds: -3600);
            repo.InsertSession(expired);

            // 過期列由查詢條件過濾，不再 delete-on-read
            Assert.Null(repo.GetSession(expired.AccessToken));
            // 讀完該列仍在，交由清理排程回收——若讀取仍會刪除，這裡就沒有東西可刪了
            Assert.True(repo.DeleteExpiredSessions() >= 1);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("DeleteExpiredSessions 應刪除過期列、保留未過期列且重複執行冪等")]
        public void DeleteExpiredSessions_RemovesOnlyExpired_AndIsIdempotent()
        {
            var repo = CreateRepo();
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
        [DisplayName("未帶 CompanyId 的種子應重建為未進公司狀態")]
        public void GetSession_SeedWithoutCompanyId_RebuildsAsCompanyLess()
        {
            var repo = CreateRepo();
            var seed = CreateSeed();

            repo.InsertSession(seed);

            Assert.Null(repo.GetSession(seed.AccessToken)!.CompanyId);
        }
    }
}
