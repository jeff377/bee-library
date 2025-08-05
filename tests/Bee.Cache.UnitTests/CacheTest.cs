using Bee.Define;

namespace Bee.Cache.UnitTests
{
    [Collection("Initialize")]
    public class CacheTest
    {
        static CacheTest()
        {
            BackendInfo.DefinePath = @"D:\DefinePath";
        }

        [Fact]
        public void SystemSettingsCache()
        {
            var settings = CacheFunc.GetSystemSettings();
            for (int i = 0; i < 10; i++)
            {
                var cache = CacheFunc.GetSystemSettings();
                Assert.Equal(settings, cache);
            }
        }

        [Fact]
        public void DatabaseSettingsCache()
        {
            var settings = CacheFunc.GetDatabaseSettings();
            for (int i = 0; i < 10; i++)
            {
                var cache = CacheFunc.GetDatabaseSettings();
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
            CacheFunc.SetSessionInfo(sessionInfo);
            var sessionInfoFromCache = CacheFunc.GetSessionInfo(sessionInfo.AccessToken);
            Assert.Equal(sessionInfo.AccessToken, sessionInfoFromCache.AccessToken);

            CacheFunc.RemoveSessionInfo(sessionInfo.AccessToken);
            sessionInfo = CacheFunc.GetSessionInfo(sessionInfo.AccessToken);
            Assert.Null(sessionInfo);
        }   
    }
}