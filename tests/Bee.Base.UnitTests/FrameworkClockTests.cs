using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// <see cref="FrameworkClock"/> 測試：時區換算、空白時區的 UTC 預設、無法解析時的失敗行為。
    /// </summary>
    /// <remarks>
    /// 期望值一律由 <see cref="TimeZoneInfo"/> 動態推導，不寫死偏移量——測試在開發機
    /// （Asia/Taipei）與 CI（UTC）下都必須成立。
    /// </remarks>
    public class FrameworkClockTests
    {
        private const string Taipei = "Asia/Taipei";

        [Fact]
        [DisplayName("Today 依指定時區回傳當地日曆日")]
        public void Today_UsesGivenZone()
        {
            var expected = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(Taipei)));

            Assert.Equal(expected, FrameworkClock.Today(Taipei));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("空白時區代表 UTC，而非機器時區")]
        public void Today_BlankZone_MeansUtc(string timeZoneId)
        {
            Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), FrameworkClock.Today(timeZoneId));
        }

        [Fact]
        [DisplayName("Now 的 Kind 一律為 Unspecified，絕不為 Local")]
        public void Now_KindIsAlwaysUnspecified()
        {
            // Local 會標錯「非本機時區的牆上時間」，且在兩條 wire 上都會位移讀數（ADR-032 D6）。
            Assert.Equal(DateTimeKind.Unspecified, FrameworkClock.Now(Taipei).Kind);
            Assert.Equal(DateTimeKind.Unspecified, FrameworkClock.Now(string.Empty).Kind);
        }

        [Fact]
        [DisplayName("Now 與 Today 對同一時區一致")]
        public void Now_AndToday_AgreeOnTheSameZone()
        {
            Assert.Equal(DateOnly.FromDateTime(FrameworkClock.Now(Taipei)), FrameworkClock.Today(Taipei));
        }

        [Fact]
        [DisplayName("時區換算的偏移量與 TimeZoneInfo 一致")]
        public void Now_AppliesTheZoneOffset()
        {
            var offset = TimeZoneInfo.FindSystemTimeZoneById(Taipei).GetUtcOffset(DateTime.UtcNow);

            var delta = FrameworkClock.Now(Taipei) - FrameworkClock.Now(string.Empty);

            Assert.True(Math.Abs((delta - offset).TotalSeconds) < 5,
                $"預期偏移 {offset}，實得 {delta}。");
        }

        [Fact]
        [DisplayName("無法解析的時區應擲例外，不得靜默退回 UTC")]
        public void Today_UnresolvableZone_Throws()
        {
            // 靜默退回 UTC 會讓每個日期都錯得無聲無息；行動端 / WASM 缺 tz 資料正是這種形態。
            var exception = Assert.Throws<InvalidOperationException>(
                () => FrameworkClock.Today("Not/AZone"));

            Assert.Contains("Not/AZone", exception.Message, StringComparison.Ordinal);
        }
    }
}
