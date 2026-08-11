using System.ComponentModel;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// 快取基底在並行 miss 下的單飛（single-flight）行為。
    /// </summary>
    /// <remarks>
    /// 斷言刻意用 <b>同一個實例</b> 而非「都不是 null」：這裡真正要防的後果是「兩個呼叫端
    /// 各拿到一份同 key 的物件」——例如 <c>SessionInfo</c>，此時經其中一份做的
    /// <c>EnterCompany</c> 對另一份不可見。只斷言非 null 的話，修正前也會通過。
    /// </remarks>
    public class CacheSingleFlightTests
    {
        private const int Threads = 32;

        private sealed class Payload
        {
            public string Key { get; init; } = string.Empty;
        }

        /// <summary>建立過程刻意變慢，讓並行呼叫確實重疊。</summary>
        private sealed class SlowObjectCache : ObjectCache<Payload>
        {
            private readonly string _key;
            private int _createCount;

            public SlowObjectCache(string key) { _key = key; }

            public int CreateCount => Volatile.Read(ref _createCount);

            protected override string GetKey() => _key;

            protected override Payload? CreateInstance()
            {
                Interlocked.Increment(ref _createCount);
                Thread.Sleep(50);
                return new Payload { Key = _key };
            }
        }

        private sealed class SlowKeyObjectCache : KeyObjectCache<Payload>
        {
            private readonly string _prefix;
            private readonly Func<string, Payload?> _factory;
            private int _createCount;

            public SlowKeyObjectCache(string prefix, Func<string, Payload?>? factory = null)
            {
                _prefix = prefix;
                _factory = factory ?? (k => new Payload { Key = k });
            }

            public int CreateCount => Volatile.Read(ref _createCount);

            protected override string GetCacheKey(string key)
                => ("CacheSingleFlightTests_" + _prefix + "_" + key).ToLowerInvariant();

            protected override Payload? CreateInstance(string key)
            {
                Interlocked.Increment(ref _createCount);
                Thread.Sleep(50);
                return _factory(key);
            }
        }

        /// <summary>讓所有執行緒在同一瞬間進入 <paramref name="body"/>，確保 miss 真的重疊。</summary>
        private static TResult[] RunConcurrently<TResult>(int count, Func<int, TResult> body)
        {
            var results = new TResult[count];
            var barrier = new Barrier(count);
            var threads = new Thread[count];

            for (int i = 0; i < count; i++)
            {
                int index = i;
                threads[i] = new Thread(() =>
                {
                    barrier.SignalAndWait();
                    results[index] = body(index);
                });
                threads[i].Start();
            }
            foreach (var t in threads) { t.Join(); }
            return results;
        }

        [Fact]
        [DisplayName("ObjectCache.Get 並行首次讀取應只建立一次，且所有呼叫端拿到同一實例")]
        public void ObjectCache_ConcurrentMiss_CreatesOnce()
        {
            var cache = new SlowObjectCache("CacheSingleFlightTests_single_" + Guid.NewGuid().ToString("N"));

            var results = RunConcurrently(Threads, _ => cache.Get());

            Assert.Equal(1, cache.CreateCount);
            Assert.All(results, r => Assert.NotNull(r));
            Assert.All(results, r => Assert.Same(results[0], r));
        }

        [Fact]
        [DisplayName("KeyObjectCache.Get 並行首次讀取同一 key 應只建立一次，且拿到同一實例")]
        public void KeyObjectCache_ConcurrentMiss_SameKey_CreatesOnce()
        {
            var cache = new SlowKeyObjectCache(Guid.NewGuid().ToString("N"));

            var results = RunConcurrently(Threads, _ => cache.Get("token-a"));

            Assert.Equal(1, cache.CreateCount);
            Assert.All(results, r => Assert.NotNull(r));
            Assert.All(results, r => Assert.Same(results[0], r));
        }

        [Fact]
        [DisplayName("不同 key 不應被單飛機制互相阻斷，各自建立各自的實例")]
        public void KeyObjectCache_DifferentKeys_EachCreatedIndependently()
        {
            var cache = new SlowKeyObjectCache(Guid.NewGuid().ToString("N"));

            // 同一批執行緒交錯取兩個 key：若單飛以「整個快取」而非「單一 key」為粒度，
            // 這裡會少建一份、且兩個 key 會拿到同一個物件。
            var results = RunConcurrently(Threads, i => cache.Get(i % 2 == 0 ? "key-x" : "key-y"));

            Assert.Equal(2, cache.CreateCount);
            Assert.Equal("key-x", results[0]!.Key);
            Assert.Equal("key-y", results[1]!.Key);
            Assert.All(results.Where((_, i) => i % 2 == 0), r => Assert.Same(results[0], r));
            Assert.All(results.Where((_, i) => i % 2 == 1), r => Assert.Same(results[1], r));
        }

        [Fact]
        [DisplayName("CreateInstance 回傳 null 時負向快取仍成立，第二次不再呼叫 CreateInstance")]
        public void KeyObjectCache_NegativeCache_StillShortCircuits()
        {
            var cache = new SlowKeyObjectCache(Guid.NewGuid().ToString("N"), _ => null);

            Assert.Null(cache.Get("missing"));
            Assert.Null(cache.Get("missing"));

            Assert.Equal(1, cache.CreateCount);
        }

        [Fact]
        [DisplayName("單飛完成後應清空在途表，後續 miss 仍會重新建立")]
        public void SingleFlight_DoesNotPinEntriesAfterCompletion()
        {
            var cache = new SlowKeyObjectCache(Guid.NewGuid().ToString("N"));

            var first = cache.Get("evictable");
            Assert.NotNull(first);
            cache.Remove("evictable");
            var second = cache.Get("evictable");

            // 若在途表沒有在 finally 清掉，第二次會拿到第一次那個 Lazy 的快取結果。
            Assert.Equal(2, cache.CreateCount);
            Assert.NotSame(first, second);
        }
    }
}
