using System.ComponentModel;
using Bee.ObjectCaching.CacheNotify;
using Bee.Tests.Shared;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="CacheNotifyRouter"/> routing semantics (no database). Uses a real
    /// <see cref="ICacheContainer"/> from the fixture only to satisfy the non-null argument; the
    /// recording eviction action does not touch it.
    /// </summary>
    public class CacheNotifyRouterTests : IClassFixture<BeeTestFixture>
    {
        private readonly ICacheContainer _container;

        public CacheNotifyRouterTests(BeeTestFixture fx)
        {
            _container = fx.GetRequiredService<ICacheContainer>();
        }

        [Fact]
        [DisplayName("已註冊 group 的 key 路由成功並傳入 entity")]
        public void TryInvoke_RegisteredGroup_InvokesWithEntity()
        {
            var router = new CacheNotifyRouter();
            string? captured = null;
            router.Register("OrgInfo", (_, entity) => captured = entity);

            bool routed = router.TryInvoke(_container, "OrgInfo:0001");

            Assert.True(routed);
            Assert.Equal("0001", captured);
        }

        [Fact]
        [DisplayName("未註冊 group 不路由")]
        public void TryInvoke_UnregisteredGroup_ReturnsFalse()
        {
            var router = new CacheNotifyRouter();
            bool invoked = false;
            router.Register("OrgInfo", (_, _) => invoked = true);

            bool routed = router.TryInvoke(_container, "FormSchema:Employee");

            Assert.False(routed);
            Assert.False(invoked);
        }

        [Fact]
        [DisplayName("沒有冒號的 key 不路由")]
        public void TryInvoke_KeyWithoutSeparator_ReturnsFalse()
        {
            var router = new CacheNotifyRouter();
            router.Register("OrgInfo", (_, _) => { });

            Assert.False(router.TryInvoke(_container, "OrgInfo"));
        }

        [Fact]
        [DisplayName("entity 內含冒號時保留首個冒號之後全部")]
        public void TryInvoke_EntityContainsSeparator_PreservesRemainder()
        {
            var router = new CacheNotifyRouter();
            string? captured = null;
            router.Register("Language", (_, entity) => captured = entity);

            bool routed = router.TryInvoke(_container, "Language:zh-TW:common");

            Assert.True(routed);
            Assert.Equal("zh-TW:common", captured);
        }

        [Fact]
        [DisplayName("group 比對不分大小寫")]
        public void TryInvoke_GroupIsCaseInsensitive()
        {
            var router = new CacheNotifyRouter();
            bool invoked = false;
            router.Register("OrgInfo", (_, _) => invoked = true);

            Assert.True(router.TryInvoke(_container, "orginfo:0001"));
            Assert.True(invoked);
        }

        [Fact]
        [DisplayName("重複註冊同一 group 以最後一次為準")]
        public void Register_SameGroupTwice_ReplacesAction()
        {
            var router = new CacheNotifyRouter();
            router.Register("OrgInfo", (_, _) => throw new InvalidOperationException("stale action"));
            string? captured = null;
            router.Register("OrgInfo", (_, entity) => captured = entity);

            router.TryInvoke(_container, "OrgInfo:0007");

            Assert.Equal("0007", captured);
        }
    }
}
