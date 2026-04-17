using System.ComponentModel;
using Bee.Base;

namespace Bee.ObjectCaching.UnitTests
{
    public class KeyObjectCacheTests
    {
        // 同時測試 IKeyObject 與一般物件兩條 Set 路徑
        private sealed class KeyedPayload : IKeyObject
        {
            public string Id { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;

            public string GetKey() => Id;
        }

        private sealed class StubKeyObjectCache : KeyObjectCache<KeyedPayload>
        {
            private readonly string _prefix;
            private readonly Func<string, KeyedPayload?> _factory;

            public StubKeyObjectCache(string prefix, Func<string, KeyedPayload?> factory)
            {
                _prefix = prefix;
                _factory = factory;
            }

            public int CreateInstanceCallCount { get; private set; }

            protected override string GetCacheKey(string key)
                => ("KeyObjectCacheTests_" + _prefix + "_" + key).ToLowerInvariant();

            protected override KeyedPayload? CreateInstance(string key)
            {
                CreateInstanceCallCount++;
                return _factory(key);
            }
        }

        // 不實作 IKeyObject，測試 Set(value) 的例外路徑
        private sealed class PlainCache : KeyObjectCache<string>
        {
            protected override string GetCacheKey(string key)
                => "KeyObjectCacheTests_plain_" + key;
        }

        [Fact]
        [DisplayName("Get 第一次呼叫 CreateInstance，第二次應由快取取得")]
        public void Get_CachesAfterFirstCall()
        {
            var prefix = Guid.NewGuid().ToString("N");
            var cache = new StubKeyObjectCache(prefix, key => new KeyedPayload { Id = key, Value = key + "_v" });

            var first = cache.Get("alpha");
            var second = cache.Get("alpha");

            Assert.NotNull(first);
            Assert.Same(first, second);
            Assert.Equal(1, cache.CreateInstanceCallCount);

            cache.Remove("alpha");
        }

        [Fact]
        [DisplayName("CreateInstance 回傳 null 時不應寫入快取")]
        public void Get_CreateInstanceReturnsNull_DoesNotCache()
        {
            var prefix = Guid.NewGuid().ToString("N");
            var cache = new StubKeyObjectCache(prefix, _ => null);

            Assert.Null(cache.Get("missing"));
            Assert.Null(cache.Get("missing"));
            Assert.Equal(2, cache.CreateInstanceCallCount);
        }

        [Fact]
        [DisplayName("Set(value) 透過 IKeyObject.GetKey 取得鍵後寫入")]
        public void Set_WithIKeyObject_UsesGetKey()
        {
            var prefix = Guid.NewGuid().ToString("N");
            var cache = new StubKeyObjectCache(prefix, _ => null);
            var payload = new KeyedPayload { Id = "beta", Value = "B" };

            cache.Set(payload);

            Assert.Same(payload, cache.Get("beta"));
            cache.Remove("beta");
        }

        [Fact]
        [DisplayName("Set(string,value) 與 Remove(string) 應正確運作")]
        public void Set_WithExplicitKey_AndRemove_Works()
        {
            var prefix = Guid.NewGuid().ToString("N");
            var cache = new StubKeyObjectCache(prefix, _ => null);
            var payload = new KeyedPayload { Id = "ignored-by-test", Value = "v" };

            cache.Set("manual", payload);
            Assert.Same(payload, cache.Get("manual"));

            cache.Remove("manual");
            Assert.Null(cache.Get("manual"));
        }

        [Fact]
        [DisplayName("Set(value) 對未實作 IKeyObject 的物件應拋例外")]
        public void Set_WithoutIKeyObject_Throws()
        {
            var cache = new PlainCache();
            Assert.Throws<InvalidOperationException>(() => cache.Set("non-key-object"));
        }
    }
}
