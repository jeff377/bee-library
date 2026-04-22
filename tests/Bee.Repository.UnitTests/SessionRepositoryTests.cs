using System.ComponentModel;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    [Collection("Initialize")]
    public class SessionRepositoryTests
    {
        [DbFact]
        [DisplayName("CreateSession 傳入有效使用者編號應建立 Session")]
        public void CreateSession_ValidUserId_CreatesSession()
        {
            var repo = new SessionRepository();
            var sessionUse = repo.CreateSession("001");
            Assert.NotNull(sessionUse);
            Assert.NotEqual(Guid.Empty, sessionUse.AccessToken);
        }

        [DbFact]
        [DisplayName("CreateSession 傳入不存在的使用者編號應擲 InvalidOperationException")]
        public void CreateSession_NonExistentUserId_ThrowsInvalidOperation()
        {
            var repo = new SessionRepository();

            Assert.Throws<InvalidOperationException>(
                () => repo.CreateSession("__nonexistent_user_xyz__"));
        }

        [DbFact]
        [DisplayName("GetSession 傳入不存在的 AccessToken 應回傳 null")]
        public void GetSession_NonExistentToken_ReturnsNull()
        {
            var repo = new SessionRepository();

            var result = repo.GetSession(Guid.NewGuid());

            Assert.Null(result);
        }

        [DbFact]
        [DisplayName("GetSession 傳入有效 Token 應回傳 SessionUser")]
        public void GetSession_ValidToken_ReturnsSessionUser()
        {
            var repo = new SessionRepository();
            var created = repo.CreateSession("001", expiresIn: 3600);

            var result = repo.GetSession(created.AccessToken);

            Assert.NotNull(result);
            Assert.Equal("001", result.UserID);
            Assert.Equal(created.AccessToken, result.AccessToken);
        }

        [DbFact]
        [DisplayName("GetSession 已過期的 Session 應回傳 null")]
        public void GetSession_ExpiredSession_ReturnsNull()
        {
            var repo = new SessionRepository();
            // Create a session that expired 1 hour ago
            var created = repo.CreateSession("001", expiresIn: -3600);

            var result = repo.GetSession(created.AccessToken);

            Assert.Null(result);
        }

        [DbFact]
        [DisplayName("GetSession 一次性 Session 取回後應被刪除")]
        public void GetSession_OneTimeSession_DeletesAfterRetrieval()
        {
            var repo = new SessionRepository();
            var created = repo.CreateSession("001", expiresIn: 3600, oneTime: true);

            var first = repo.GetSession(created.AccessToken);
            var second = repo.GetSession(created.AccessToken);

            Assert.NotNull(first);
            Assert.Null(second);
        }
    }
}
