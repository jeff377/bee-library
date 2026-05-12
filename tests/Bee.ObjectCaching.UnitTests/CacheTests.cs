using System.ComponentModel;
using Bee.Definition.Identity;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.ObjectCaching.UnitTests
{
    [Collection("Initialize")]
    public class CacheTests
    {
        [Fact]
        [DisplayName("多次取得系統設定應回傳相同的快取實例")]
        public void GetSystemSettings_CalledMultipleTimes_ReturnsSameCachedInstance()
        {
            var defineAccess = BeeTestServices.GetRequiredService<IDefineAccess>();
            var settings = defineAccess.GetSystemSettings();
            for (int i = 0; i < 10; i++)
            {
                var cache = defineAccess.GetSystemSettings();
                Assert.Equal(settings, cache);
            }
        }

        [Fact]
        [DisplayName("多次取得資料庫設定應回傳相同的快取實例")]
        public void GetDatabaseSettings_CalledMultipleTimes_ReturnsSameCachedInstance()
        {
            var defineAccess = BeeTestServices.GetRequiredService<IDefineAccess>();
            var settings = defineAccess.GetDatabaseSettings();
            for (int i = 0; i < 10; i++)
            {
                var cache = defineAccess.GetDatabaseSettings();
                Assert.Equal(settings, cache);
            }
        }

        [Fact]
        [DisplayName("Session 快取設定後應可取得，移除後應回傳 null")]
        public void SessionInfo_SetAndRemove_BehavesCorrectly()
        {
            var sessionService = BeeTestServices.GetRequiredService<ISessionInfoService>();
            var sessionInfo = new SessionInfo
            {
                AccessToken = Guid.NewGuid(),
                UserId = "test_user",
                UserName = "Test User"
            };
            sessionService.Set(sessionInfo);
            var sessionInfoFromCache = CacheContainer.SessionInfo.Get(sessionInfo.AccessToken);
            Assert.NotNull(sessionInfoFromCache);
            Assert.Equal(sessionInfo.AccessToken, sessionInfoFromCache!.AccessToken);

            sessionService.Remove(sessionInfo.AccessToken);
            sessionInfo = sessionService.Get(sessionInfo.AccessToken);
            Assert.Null(sessionInfo);
        }
    }
}
