using System.ComponentModel;
using Bee.LoadTests.Statistics;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for <see cref="LatencyStatistics"/>.
    /// </summary>
    public class LatencyStatisticsTests
    {
        [Fact]
        [DisplayName("百分位採 nearest-rank：p50 取第 5 個樣本")]
        public void Percentile_TenSamples_UsesNearestRank()
        {
            var stats = LatencyStatistics.FromMilliseconds(
                [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

            // ceil(50 / 100 * 10) = 5, so the p50 is the 5th value in sorted order.
            Assert.Equal(5, stats.Percentile(50));
        }

        [Fact]
        [DisplayName("百分位回傳實際觀測值，不做內插")]
        public void Percentile_P99OfHundred_ReturnsObservedValue()
        {
            var samples = Enumerable.Range(1, 100).Select(i => (double)i).ToArray();
            var stats = LatencyStatistics.FromMilliseconds(samples);

            Assert.Equal(99, stats.Percentile(99));
            Assert.Equal(100, stats.Percentile(100));
        }

        [Fact]
        [DisplayName("輸入未排序時仍算出正確百分位")]
        public void FromMilliseconds_UnsortedInput_SortsBeforeComputing()
        {
            var stats = LatencyStatistics.FromMilliseconds([9, 1, 7, 3, 5]);

            Assert.Equal(1, stats.Min);
            Assert.Equal(9, stats.Max);
            Assert.Equal(5, stats.Percentile(50));
        }

        [Fact]
        [DisplayName("不更動呼叫端傳入的集合")]
        public void FromMilliseconds_DoesNotMutateCallerArray()
        {
            double[] source = [3, 1, 2];

            LatencyStatistics.FromMilliseconds(source);

            Assert.Equal([3, 1, 2], source);
        }

        [Fact]
        [DisplayName("空樣本回傳零而不擲例外")]
        public void Percentile_NoSamples_ReturnsZero()
        {
            var stats = LatencyStatistics.FromMilliseconds([]);

            Assert.Equal(0, stats.Count);
            Assert.Equal(0, stats.Percentile(95));
            Assert.Equal(0, stats.Min);
            Assert.Equal(0, stats.Max);
            Assert.Equal(0, stats.Mean);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
        [DisplayName("百分位超出 0~100 範圍時擲例外")]
        public void Percentile_OutOfRange_Throws(double percentile)
        {
            var stats = LatencyStatistics.FromMilliseconds([1, 2, 3]);

            Assert.Throws<ArgumentOutOfRangeException>(() => stats.Percentile(percentile));
        }

        [Fact]
        [DisplayName("平均值取所有樣本的算術平均")]
        public void Mean_ReturnsArithmeticMean()
        {
            var stats = LatencyStatistics.FromMilliseconds([2, 4, 6, 8]);

            Assert.Equal(5, stats.Mean);
        }
    }
}
