using System.ComponentModel;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    [Collection("Initialize")]
    public class SessionRepositoryTests
    {
        [LocalOnlyFact]
        [DisplayName("CreateSession 傳入有效使用者編號應建立 Session")]
        public void CreateSession_ValidUserId_CreatesSession()
        {
            var repo = new SessionRepository();
            var sessionUse = repo.CreateSession("001");
        }
    }
}
