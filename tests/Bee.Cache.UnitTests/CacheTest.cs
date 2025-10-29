using Bee.Define;

namespace Bee.Cache.UnitTests
{
    [Collection("Initialize")]
    public class CacheTest
    {
        static CacheTest()
        {
        }

        [Fact]
        public void SystemSettingsCache()
        {
            var settings = BackendInfo.DefineAccess.GetSystemSettings();
            for (int i = 0; i < 10; i++)
            {
                var cache = BackendInfo.DefineAccess.GetSystemSettings();
                Assert.Equal(settings, cache);
            }
        }

        [Fact]
        public void DatabaseSettingsCache()
        {
            var settings = BackendInfo.DefineAccess.GetDatabaseSettings();
            for (int i = 0; i < 10; i++)
            {
                var cache = BackendInfo.DefineAccess.GetDatabaseSettings();
                Assert.Equal(settings, cache);
            }
        }
        
        [Fact]
        public void SessionInfoCache()
        {
            var sessionInfo = new SessionInfo
            {
                AccessToken = Guid.NewGuid(),
                UserId = "test_user",
                UserName = "Test User"
            };
            BackendInfo.SessionInfoService.Set(sessionInfo);
            var sessionInfoFromCache = CacheFunc.GetSessionInfo(sessionInfo.AccessToken);
            Assert.Equal(sessionInfo.AccessToken, sessionInfoFromCache.AccessToken);

            BackendInfo.SessionInfoService.Remove(sessionInfo.AccessToken);
            sessionInfo = BackendInfo.SessionInfoService.Get(sessionInfo.AccessToken);
            Assert.Null(sessionInfo);
        }   
    }
}