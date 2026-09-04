using System.ComponentModel;
using System.Reflection;
using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Core.UnitTests.JsonRpc
{
    /// <summary>
    /// <see cref="MemoryReplayWindowStore"/>：出貨預設的重放視窗儲存。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 這個型別先前**零測試** —— 全 `tests/` 對它與 <c>IReplayWindowStore</c> 的參照數是 0，
    /// 而它是每個預設部署都在用的 per-session map。它甚至公開了 <c>Count</c> 並註明
    /// 「intended for tests and diagnostics」，卻沒有任何測試用它。
    /// </para>
    /// <para>
    /// 掛 <c>ApiServiceOptionsState</c>：淘汰期由
    /// <see cref="ApiServiceOptions.WireFrameTimestampTolerance"/> 推導，本類別會改寫它。
    /// </para>
    /// </remarks>
    [Collection("ApiServiceOptionsState")]
    public class MemoryReplayWindowStoreTests
    {
        [Fact]
        [DisplayName("同一個 token 應回同一個 window（序號歷史才不會遺失）")]
        public void GetOrAdd_SameToken_ReturnsSameWindow()
        {
            var store = new MemoryReplayWindowStore();
            var token = Guid.NewGuid();

            var first = store.GetOrAdd(token);
            Assert.True(first.TryAccept(7));

            var second = store.GetOrAdd(token);

            Assert.Same(first, second);
            // 歷史留著才擋得住重放 —— 這正是「回同一個實例」的意義所在。
            Assert.False(second.TryAccept(7));
        }

        [Fact]
        [DisplayName("不同 token 的 window 互不干擾")]
        public void GetOrAdd_DifferentTokens_AreIsolated()
        {
            var store = new MemoryReplayWindowStore();

            Assert.True(store.GetOrAdd(Guid.NewGuid()).TryAccept(1));
            Assert.True(store.GetOrAdd(Guid.NewGuid()).TryAccept(1));
            Assert.Equal(2, store.Count);
        }

        [Fact]
        [DisplayName("回歸：entry 建立當下就必須帶時間戳，不得留給呼叫端補")]
        public void Entry_IsStampedAtConstruction_NotAfterwards()
        {
            // Entry.LastTouchedMs 曾預設為 0：GetOrAdd 建立它之後才寫入真正的時間戳，
            // 落在那個空隙的 sweep 會讀到 0、判定比任何 cutoff 都舊而移除一個正在使用的
            // window。該 session 的下一個請求就會拿到全新的 window（沒有任何歷史）——
            // 也就是重放防護在那個 session 上靜默重置一次。
            //
            // 為什麼用反射而不是行為測試：這個 race 沒辦法用行為乾淨隔離。要撞到它就得讓
            // sweep 跑得夠頻繁（把淘汰期調到毫秒級），但那樣一來執行緒被排程延遲超過淘汰期
            // 就是**合法**淘汰，測試會在修正在位時也紅。實測過並行版，三次有一次紅。
            // 不變式本身沒有其他可觀察面，所以直接釘它。
            var entryType = typeof(MemoryReplayWindowStore)
                .GetNestedType("Entry", BindingFlags.NonPublic);
            Assert.NotNull(entryType);

            var field = entryType!.GetField("LastTouchedMs", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);

            var entry = Activator.CreateInstance(entryType, nonPublic: true);
            long stamped = (long)field!.GetValue(entry)!;

            Assert.True(stamped > 0,
                "Entry 建立時沒有蓋上時間戳。sweep 會把它當成比任何 cutoff 都舊而移除一個" +
                "正在使用的 window，該 session 的重放防護就此重置一次。");
        }

        [Fact]
        [DisplayName("閒置超過淘汰期的 entry 應被掃掉（記憶體有界）")]
        public void SweepIfDue_IdleEntries_AreEvicted()
        {
            var previous = ApiServiceOptions.WireFrameTimestampTolerance;
            try
            {
                ApiServiceOptions.WireFrameTimestampTolerance = TimeSpan.FromMilliseconds(1);
                var store = new MemoryReplayWindowStore();

                for (int i = 0; i < 50; i++) { store.GetOrAdd(Guid.NewGuid()); }
                int seeded = store.Count;
                Assert.True(seeded > 0, "前提：先種進去的 entry 確實存在，否則下面的斷言恆真。");

                Thread.Sleep(30);   // > 淘汰期（tolerance × 2 = 2ms）
                store.GetOrAdd(Guid.NewGuid());   // sweep 由存取觸發，不是背景計時器

                // 掃過之後只該剩下剛剛那一個；不寫死數字以免對 sweep 的內部節流過度耦合。
                Assert.True(store.Count < seeded,
                    $"閒置的 entry 沒有被淘汰：掃描前 {seeded}、掃描後 {store.Count}。");
            }
            finally
            {
                ApiServiceOptions.WireFrameTimestampTolerance = previous;
            }
        }
    }
}
