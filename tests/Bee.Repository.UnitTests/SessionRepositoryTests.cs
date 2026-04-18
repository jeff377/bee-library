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
    }
}
