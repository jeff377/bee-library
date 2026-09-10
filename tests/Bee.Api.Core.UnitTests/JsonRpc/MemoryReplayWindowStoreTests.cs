using System.ComponentModel;
using System.Reflection;
using Bee.Api.Core.JsonRpc;
using Microsoft.Extensions.Time.Testing;

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
    /// 時間一律由 <c>FakeTimeProvider</c> 供給（走 <c>internal</c> 建構子，本組件已宣告
    /// <c>InternalsVisibleTo</c>），所以本類別**不改寫任何 production static**，也不需要
    /// <c>ApiServiceOptionsState</c> collection —— 淘汰期直接由現行
    /// <see cref="ApiServiceOptions.WireFrameTimestampTolerance"/> 推導後 <c>Advance</c> 過去，
    /// 不花任何真實時間。這同時是淘汰邊界與 sweep 節流得以精確斷言的前提。
    /// </para>
    /// </remarks>
    public class MemoryReplayWindowStoreTests
    {
        /// <summary>淘汰期：與受測型別同一條推導，避免在測試裡寫死另一個數字。</summary>
        private static TimeSpan Lifetime => ApiServiceOptions.WireFrameTimestampTolerance * 2;

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
        [DisplayName("回歸：entry 的時間戳必須由建構子強制帶入，不得留給呼叫端補")]
        public void Entry_RequiresTimestampAtConstruction()
        {
            // Entry.LastTouchedTimestamp 曾預設為 0：GetOrAdd 建立它之後才寫入真正的時間戳，
            // 落在那個空隙的 sweep 會讀到 0、判定比任何 cutoff 都舊而移除一個正在使用的
            // window。該 session 的下一個請求就會拿到全新的 window（沒有任何歷史）——
            // 也就是重放防護在那個 session 上靜默重置一次。
            //
            // 為什麼用反射而不是行為測試：這個 race 沒辦法用行為乾淨隔離。要撞到它就得讓
            // sweep 與 entry 插入交錯，而假時鐘只凍結時間、不排程執行緒；改用真實時間把
            // 淘汰期壓到毫秒級，則執行緒被排程延遲超過淘汰期就是**合法**淘汰，測試會在
            // 修正在位時也紅。實測過並行版，三次有一次紅。
            //
            // 改由建構子參數承載之後，不變式從「執行期靠人記得蓋章」升級為「編譯期無從違反」，
            // 所以這裡釘的是型別形狀：沒有無參數建構子可用，就沒有留 0 的寫法。
            var entryType = typeof(MemoryReplayWindowStore)
                .GetNestedType("Entry", BindingFlags.NonPublic);
            Assert.NotNull(entryType);

            var constructors = entryType!.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var only = Assert.Single(constructors);
            var parameter = Assert.Single(only.GetParameters());
            Assert.Equal(typeof(long), parameter.ParameterType);
        }

        [Fact]
        [DisplayName("閒置超過淘汰期的 entry 應被掃掉（記憶體有界）")]
        public void SweepIfDue_IdleEntries_AreEvicted()
        {
            var clock = new FakeTimeProvider();
            var store = new MemoryReplayWindowStore(clock);

            for (int i = 0; i < 50; i++) { store.GetOrAdd(Guid.NewGuid()); }
            Assert.Equal(50, store.Count);

            clock.Advance(Lifetime + TimeSpan.FromTicks(1));
            store.GetOrAdd(Guid.NewGuid());   // sweep 由存取觸發，不是背景計時器

            // 時間是假的，所以這裡可以寫死：50 筆全過期、只剩剛剛加進去的那一個。
            Assert.Equal(1, store.Count);
        }

        [Fact]
        [DisplayName("閒置恰好等於淘汰期的 entry 要留著（淘汰邊界是嚴格大於）")]
        public void SweepIfDue_IdleExactlyLifetime_AreKept()
        {
            var clock = new FakeTimeProvider();
            var store = new MemoryReplayWindowStore(clock);

            for (int i = 0; i < 50; i++) { store.GetOrAdd(Guid.NewGuid()); }

            // 剛好走完一個淘汰期：sweep 會跑（節流也是同一個長度），但一筆都不該掃掉。
            clock.Advance(Lifetime);
            store.GetOrAdd(Guid.NewGuid());

            Assert.Equal(51, store.Count);
        }

        [Fact]
        [DisplayName("節流：距上次 sweep 不足一個淘汰期時不掃，補滿才掃")]
        public void SweepIfDue_WithinThrottleWindow_DoesNotSweep()
        {
            var clock = new FakeTimeProvider();
            var store = new MemoryReplayWindowStore(clock);

            for (int i = 0; i < 50; i++) { store.GetOrAdd(Guid.NewGuid()); }

            // 先走滿一個淘汰期觸發一次 sweep，把節流的計時原點推到「此刻」。
            // 那 50 筆此時閒置恰好等於淘汰期，還留著（邊界由另一筆測試釘）。
            clock.Advance(Lifetime);
            store.GetOrAdd(Guid.NewGuid());
            Assert.Equal(51, store.Count);

            // 再走「差 1 tick 就滿下一個淘汰期」：那 50 筆已閒置近兩倍淘汰期、**早該被掃掉**，
            // 但距上次 sweep 還差 1 tick，於是這次存取根本不掃。
            clock.Advance(Lifetime - TimeSpan.FromTicks(1));
            store.GetOrAdd(Guid.NewGuid());
            Assert.Equal(52, store.Count);

            // 對照組：只再補 1 tick，那 50 筆立刻消失。可見上一步留著它們的是節流，
            // 不是「還沒到期」—— 否則這 1 tick 不足以造成任何差別。
            clock.Advance(TimeSpan.FromTicks(1));
            store.GetOrAdd(Guid.NewGuid());
            Assert.Equal(3, store.Count);
        }

        [Fact]
        [DisplayName("持續使用中的 session 永不被淘汰（重放防護不得靜默重置）")]
        public void GetOrAdd_ActiveToken_IsNeverEvicted()
        {
            var clock = new FakeTimeProvider();
            var store = new MemoryReplayWindowStore(clock);
            var active = Guid.NewGuid();

            var window = store.GetOrAdd(active);
            Assert.True(window.TryAccept(42));

            // 累計遠超過淘汰期，但每一輪都碰到它 —— GetOrAdd 會刷新時間戳，所以它始終不算閒置。
            for (int round = 0; round < 10; round++)
            {
                clock.Advance(Lifetime * 0.9);
                Assert.Same(window, store.GetOrAdd(active));
            }

            // window 若被換掉，這裡會接受 42 而通過 —— 那正是重放防護被靜默重置的樣子。
            Assert.False(store.GetOrAdd(active).TryAccept(42));
        }
    }
}
