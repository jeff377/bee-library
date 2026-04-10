using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    [Collection("Initialize")]
    public class SessionRepositoryTests
    {
        [LocalOnlyFact]
        public void CreateSession()
        {            
            var repo = new SessionRepository();
            var sessionUse = repo.CreateSession("001");
        }
    }
}
