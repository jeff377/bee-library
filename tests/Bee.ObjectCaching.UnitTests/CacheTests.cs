using System.ComponentModel;
using Bee.Definition.Identity;
using Bee.Definition.Storage;
using Bee.Tests.Shared;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// 透過 fixture 的 DI 容器執行快取行為測試。PR 5.7 後 cache 改為接 <c>PathOptions</c> 注入，
    /// 不再走 process-wide static，可與其他 test class 平行執行。
    /// </summary>
    public class CacheTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public CacheTests(BeeTestFixture fx)
        {
            _fx = fx;
        }

        [Fact]
        [DisplayName("多次取得系統設定應回傳相同的快取實例")]
        public void GetSystemSettings_CalledMultipleTimes_ReturnsSameCachedInstance()
        {
            var defineAccess = _fx.GetRequiredService<IDefineAccess>();
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
            var defineAccess = _fx.GetRequiredService<IDefineAccess>();
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
            var sessionService = _fx.GetRequiredService<ISessionInfoService>();
            var cache = _fx.GetRequiredService<ICacheContainer>();
            var sessionInfo = new SessionInfo
            {
                AccessToken = Guid.NewGuid(),
                UserId = "test_user",
                UserName = "Test User"
            };
            sessionService.Set(sessionInfo);

            // 透過 fixture 的 ICacheContainer 讀取（共用 fixture 的 cache key prefix）；
            // 直接走 process-wide CacheContainer.SessionInfo 在 prefix 隔離後看不到資料。
            var sessionInfoFromCache = cache.SessionInfo.Get(sessionInfo.AccessToken);
            Assert.NotNull(sessionInfoFromCache);
            Assert.Equal(sessionInfo.AccessToken, sessionInfoFromCache!.AccessToken);

            sessionService.Remove(sessionInfo.AccessToken);
            Assert.Null(sessionService.Get(sessionInfo.AccessToken));
        }
    }
}
