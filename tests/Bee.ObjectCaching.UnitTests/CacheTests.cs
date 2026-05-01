using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Identity;

namespace Bee.ObjectCaching.UnitTests
{
    [Collection("Initialize")]
    public class CacheTests
    {
        static CacheTests()
        {
        }

        [Fact]
        [DisplayName("多次取得系統設定應回傳相同的快取實例")]
        public void GetSystemSettings_CalledMultipleTimes_ReturnsSameCachedInstance()
        {
            var settings = BackendInfo.DefineAccess.GetSystemSettings();
            for (int i = 0; i < 10; i++)
            {
                var cache = BackendInfo.DefineAccess.GetSystemSettings();
                Assert.Equal(settings, cache);
            }
        }

        [Fact]
        [DisplayName("多次取得資料庫設定應回傳相同的快取實例")]
        public void GetDatabaseSettings_CalledMultipleTimes_ReturnsSameCachedInstance()
        {
            var settings = BackendInfo.DefineAccess.GetDatabaseSettings();
            for (int i = 0; i < 10; i++)
            {
                var cache = BackendInfo.DefineAccess.GetDatabaseSettings();
                Assert.Equal(settings, cache);
            }
        }

        [Fact]
        [DisplayName("Session 快取設定後應可取得，移除後應回傳 null")]
        public void SessionInfo_SetAndRemove_BehavesCorrectly()
        {
            var sessionInfo = new SessionInfo
            {
                AccessToken = Guid.NewGuid(),
                UserId = "test_user",
                UserName = "Test User"
            };
            BackendInfo.SessionInfoService.Set(sessionInfo);
            var sessionInfoFromCache = CacheContainer.SessionInfo.Get(sessionInfo.AccessToken);
            Assert.NotNull(sessionInfoFromCache);
            Assert.Equal(sessionInfo.AccessToken, sessionInfoFromCache!.AccessToken);

            BackendInfo.SessionInfoService.Remove(sessionInfo.AccessToken);
            sessionInfo = BackendInfo.SessionInfoService.Get(sessionInfo.AccessToken);
            Assert.Null(sessionInfo);
        }
    }
}
