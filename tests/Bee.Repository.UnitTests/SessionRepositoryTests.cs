namespace Bee.Repository.UnitTests
{
    [Collection("Initialize")]
    public class SessionRepositoryTests
    {
        [Fact]
        public void CreateSession()
        {            
            var repo = new SessionRepository();
            var sessionUse = repo.CreateSession("001");
        }
    }
}
