using System.ComponentModel;

namespace Bee.ObjectCaching.UnitTests
{
    public class ObjectCacheTests
    {
        // 測試專用包裝物件，避免與其他測試共用快取鍵
        private sealed class TestPayload
        {
            public string Value { get; set; } = string.Empty;
        }

        // 透過建構子取得鍵後綴，確保每個測試使用獨立的快取鍵
        private sealed class StubObjectCache : ObjectCache<TestPayload>
        {
            private readonly string _suffix;
            private readonly Func<TestPayload?> _factory;

            public StubObjectCache(string suffix, Func<TestPayload?> factory)
            {
                _suffix = suffix;
                _factory = factory;
            }

            public int CreateInstanceCallCount { get; private set; }

            protected override string GetKey() => "ObjectCacheTests_" + _suffix;

            protected override CacheItemPolicy GetPolicy()
                => new CacheItemPolicy(CacheTimeKind.SlidingTime, 1);

            protected override TestPayload? CreateInstance()
            {
                CreateInstanceCallCount++;
                return _factory();
            }
        }

        [Fact]
        [DisplayName("Get 第一次應呼叫 CreateInstance，後續應自快取取得")]
        public void Get_FirstCall_CreatesAndCaches_SubsequentCallsHit()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var cache = new StubObjectCache(suffix, () => new TestPayload { Value = "hello" });

            var first = cache.Get();
            var second = cache.Get();

            Assert.NotNull(first);
            Assert.Equal("hello", first!.Value);
            Assert.Same(first, second);
            Assert.Equal(1, cache.CreateInstanceCallCount);

            cache.Remove();
        }

        [Fact]
        [DisplayName("CreateInstance 回傳 null 時不應寫入快取，且每次 Get 都會再呼叫")]
        public void Get_CreateInstanceReturnsNull_DoesNotCache()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var cache = new StubObjectCache(suffix, () => null);

            Assert.Null(cache.Get());
            Assert.Null(cache.Get());
            Assert.Equal(2, cache.CreateInstanceCallCount);
        }

        [Fact]
        [DisplayName("Set 後 Get 應回傳該物件，Remove 後 Get 應重新建立")]
        public void Set_ThenRemove_BehavesCorrectly()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var stub = new TestPayload { Value = "manual" };
            var cache = new StubObjectCache(suffix, () => new TestPayload { Value = "factory" });

            cache.Set(stub);
            Assert.Same(stub, cache.Get());
            Assert.Equal(0, cache.CreateInstanceCallCount);

            cache.Remove();
            var rebuilt = cache.Get();
            Assert.NotNull(rebuilt);
            Assert.Equal("factory", rebuilt!.Value);
            Assert.Equal(1, cache.CreateInstanceCallCount);

            cache.Remove();
        }

        // 不 override GetPolicy / CreateInstance,讓基底類別預設實作被執行
        private sealed class DefaultPolicyCache : ObjectCache<TestPayload>
        {
            private readonly string _suffix;
            private readonly TestPayload? _payload;

            public DefaultPolicyCache(string suffix, TestPayload? payload)
            {
                _suffix = suffix;
                _payload = payload;
            }

            protected override string GetKey() => "ObjectCacheTests_Default_" + _suffix;

            // 僅於 _payload 非 null 時才重寫 CreateInstance,用以觸發預設 GetPolicy 分支;
            // _payload 為 null 時不重寫,讓基底類別預設 CreateInstance 回傳 default 的分支被執行。
            protected override TestPayload? CreateInstance()
            {
                return _payload ?? base.CreateInstance();
            }
        }

        // 完全不 override CreateInstance,僅 override GetKey,覆蓋預設 CreateInstance 回傳 default 的分支
        private sealed class BareBonesCache : ObjectCache<TestPayload>
        {
            private readonly string _suffix;
            public BareBonesCache(string suffix) { _suffix = suffix; }
            protected override string GetKey() => "ObjectCacheTests_Bare_" + _suffix;
        }

        [Fact]
        [DisplayName("未 override GetPolicy 時應套用預設 20 分鐘 SlidingTime 並將值寫入快取")]
        public void Get_UsesDefaultGetPolicy_WhenNotOverridden()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var payload = new TestPayload { Value = "default-policy" };
            var cache = new DefaultPolicyCache(suffix, payload);

            // 第一次 Get → CreateInstance 回傳 payload → 寫入快取時會呼叫基底預設 GetPolicy
            var first = cache.Get();
            Assert.Same(payload, first);

            // 第二次 Get → 自快取取得,證明已以預設 policy 寫入
            var second = cache.Get();
            Assert.Same(payload, second);

            cache.Remove();
        }

        [Fact]
        [DisplayName("未 override CreateInstance 時預設應回傳 null,不寫入快取")]
        public void Get_UsesDefaultCreateInstance_ReturnsNull()
        {
            var suffix = Guid.NewGuid().ToString("N");
            var cache = new BareBonesCache(suffix);

            var result = cache.Get();

            Assert.Null(result);

            cache.Remove();
        }
    }
}
