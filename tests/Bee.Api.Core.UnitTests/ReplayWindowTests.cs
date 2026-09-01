using System.Collections.Concurrent;
using System.ComponentModel;
using Bee.Api.Core.JsonRpc;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// ReplayWindow 滑動視窗演算法測試（純邏輯，不觸及 process-wide 狀態）。
    /// </summary>
    public class ReplayWindowTests
    {
        [Fact]
        [DisplayName("首次序號不論大小皆應接受並成為基準")]
        public void TryAccept_FirstSequence_IsAccepted()
        {
            var window = new ReplayWindow();

            Assert.True(window.TryAccept(5_000));
        }

        [Fact]
        [DisplayName("重複的序號應被拒")]
        public void TryAccept_RepeatedSequence_IsRejected()
        {
            var window = new ReplayWindow();
            window.TryAccept(10);

            Assert.False(window.TryAccept(10));
        }

        [Fact]
        [DisplayName("遞增序號應全部接受")]
        public void TryAccept_IncreasingSequences_AllAccepted()
        {
            var window = new ReplayWindow();

            for (long i = 1; i <= 500; i++)
            {
                Assert.True(window.TryAccept(i), $"sequence {i} 應被接受");
            }
        }

        [Fact]
        [DisplayName("視窗內亂序到達應全部接受（並行取號但送達順序不保證）")]
        public void TryAccept_OutOfOrderWithinWindow_AllAccepted()
        {
            // 取號是原子的，但多個 connector 並行送出時到達順序不固定。
            // 嚴格遞增會誤殺這種正常流量，因此視窗必須容忍亂序。
            var window = new ReplayWindow();
            window.TryAccept(100);

            Assert.True(window.TryAccept(98));
            Assert.True(window.TryAccept(99));
            Assert.True(window.TryAccept(101));
        }

        [Fact]
        [DisplayName("落在視窗之後的舊序號應被拒")]
        public void TryAccept_SequenceBehindWindow_IsRejected()
        {
            var window = new ReplayWindow();
            window.TryAccept(ReplayWindow.WindowSize + 10);

            Assert.False(window.TryAccept(9));
        }

        [Fact]
        [DisplayName("前跳超過視窗寬度應清空位圖，先前的序號不再可用")]
        public void TryAccept_JumpBeyondWindow_ClearsEarlierSlots()
        {
            var window = new ReplayWindow();
            window.TryAccept(1);
            window.TryAccept(2);

            Assert.True(window.TryAccept(2 + ReplayWindow.WindowSize));
            Assert.False(window.TryAccept(2));
        }

        [Fact]
        [DisplayName("前跳恰好等於上限應接受")]
        public void TryAccept_JumpExactlyAtLimit_IsAccepted()
        {
            var window = new ReplayWindow();
            window.TryAccept(1);

            Assert.True(window.TryAccept(1 + ReplayWindow.MaxForwardJump));
        }

        [Fact]
        [DisplayName("前跳超過上限應被拒，避免一次算錯就讓 session 卡死")]
        public void TryAccept_JumpBeyondLimit_IsRejected()
        {
            // 沒有上限的話，用戶端一次整數運算失誤送出接近 long.MaxValue 的序號，
            // 該 session 之後所有正常請求都會落在視窗外而被拒——token 有效、
            // 金鑰正確卻全部失敗，極難診斷。
            var window = new ReplayWindow();
            window.TryAccept(1);

            Assert.False(window.TryAccept(long.MaxValue));
            Assert.True(window.TryAccept(2));
        }

        [Fact]
        [DisplayName("負數序號應被拒")]
        public void TryAccept_NegativeSequence_IsRejected()
        {
            var window = new ReplayWindow();

            Assert.False(window.TryAccept(-1));
        }

        [Fact]
        [DisplayName("同一序號並行送入應只被接受一次")]
        public void TryAccept_SameSequenceConcurrently_AcceptedExactlyOnce()
        {
            // 視窗會被同一 session 的並行請求共用，read-modify-write 必須是原子的。
            var window = new ReplayWindow();
            var results = new ConcurrentBag<bool>();

            Parallel.For(0, 200, _ => results.Add(window.TryAccept(7)));

            Assert.Single(results, accepted => accepted);
        }

        [Fact]
        [DisplayName("視窗寬度內的不同序號並行送入應全部接受")]
        public void TryAccept_DistinctSequencesConcurrently_AllAccepted()
        {
            var window = new ReplayWindow();
            var results = new ConcurrentBag<bool>();

            Parallel.For(0, ReplayWindow.WindowSize, i => results.Add(window.TryAccept(i)));

            Assert.Equal(ReplayWindow.WindowSize, results.Count(accepted => accepted));
        }
    }
}
