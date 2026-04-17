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
    }
}
