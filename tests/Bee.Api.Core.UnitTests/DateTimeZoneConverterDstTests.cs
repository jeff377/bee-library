using System.ComponentModel;
using System.Data;
using Bee.Api.Core.JsonRpc;
using Bee.Base.Data;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// <see cref="DateTimeZoneConverter"/> 在觀測 DST 的時區下的行為。既有測試只用
    /// <c>Asia/Taipei</c> 與 <c>Pacific/Kiritimati</c>，兩者都是固定偏移、不觀測 DST。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 請求方向只剩過濾條件值會從使用者時區換算成 UTC，所以 spring-forward 缺口透過
    /// <see cref="DateTimeZoneConverter.ConvertFilterValue"/> 驗證。<c>DataSet</c> 在請求方向不換算，
    /// fall-back 重疊時段「讀進再存回」的正確性改由伺服端讀回資料庫值保證，見
    /// <c>DateTimeZoneDstSaveRoundTripTests</c>。
    /// </para>
    /// <para>
    /// 測試不寫死轉換日期是否為邊界，而是先以 <c>TimeZoneInfo.IsInvalidTime</c> 斷言前提成立——
    /// 若 runtime 的 tzdata 與預期不同，測試會在前提處明確失敗，而不是以錯誤的理由通過。
    /// </para>
    /// </remarks>
    public class DateTimeZoneConverterDstTests
    {
        private const string NewYork = "America/New_York";

        private static TimeZoneInfo Zone => TimeZoneInfo.FindSystemTimeZoneById(NewYork);

        // 2026 美國 DST：3/8 02:00 前進（02:00–02:59 不存在）。
        private static readonly DateTime s_springForwardGap = new(2026, 3, 8, 2, 30, 0, DateTimeKind.Unspecified);

        private static DataTable BuildTableWithInstant(DateTime value)
        {
            var table = new DataTable("events");
            table.AddColumn("occurred_at", FieldDbType.DateTime);
            table.Rows.Add(value);
            table.AcceptChanges();
            return table;
        }

        [Fact]
        [DisplayName("前提：2026-03-08 02:30 在 America/New_York 確實是不存在的時刻")]
        public void Precondition_SpringForwardGapIsInvalid()
        {
            Assert.True(Zone.IsInvalidTime(s_springForwardGap));
        }

        [Fact]
        [DisplayName("UtcToUser 在 DST 生效前後應套用不同偏移，而非固定偏移")]
        public void UtcToUser_HonoursDstOffsetChange()
        {
            // 同一天的 UTC 06:00：DST 前為 EST（UTC-5）、DST 後為 EDT（UTC-4）。
            var beforeUtc = new DateTime(2026, 3, 8, 6, 0, 0, DateTimeKind.Unspecified);
            var afterUtc = new DateTime(2026, 3, 8, 8, 0, 0, DateTimeKind.Unspecified);

            var converted = DateTimeZoneConverter.UtcToUser(
                BuildTableWithInstant(beforeUtc), NewYork);
            var convertedAfter = DateTimeZoneConverter.UtcToUser(
                BuildTableWithInstant(afterUtc), NewYork);

            Assert.NotNull(converted);
            Assert.NotNull(convertedAfter);

            var beforeOffset = beforeUtc - (DateTime)converted.Rows[0]["occurred_at"];
            var afterOffset = afterUtc - (DateTime)convertedAfter.Rows[0]["occurred_at"];

            Assert.NotEqual(beforeOffset, afterOffset);
            Assert.Equal(TimeSpan.FromHours(1), beforeOffset - afterOffset);
        }

        [Fact]
        [DisplayName("過濾條件值落在 spring-forward 缺口內時應前推一個 DST 差，不擲例外")]
        public void ConvertFilterValue_InvalidLocalTime_SkipsGapForward()
        {
            var converted = (DateTime)DateTimeZoneConverter.ConvertFilterValue(s_springForwardGap, NewYork, toUtc: true)!;

            // 02:30 不存在 → 前推該次轉換的 delta（此區為 1 小時）後的 03:30 才是真實時刻。
            var gap = Zone.GetUtcOffset(s_springForwardGap.Date.AddDays(1))
                      - Zone.GetUtcOffset(s_springForwardGap.Date.AddDays(-1));
            var expected = DateTime.SpecifyKind(
                TimeZoneInfo.ConvertTimeToUtc(s_springForwardGap.Add(gap), Zone), DateTimeKind.Unspecified);

            Assert.Equal(expected, converted);
        }

        [Fact]
        [DisplayName("前推後的過濾條件值轉回使用者時區應落在缺口之後，且是存在的時刻")]
        public void ConvertFilterValue_InvalidLocalTime_ResultRoundTripsToARealTime()
        {
            var utc = (DateTime)DateTimeZoneConverter.ConvertFilterValue(s_springForwardGap, NewYork, toUtc: true)!;
            var backInZone = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utc, DateTimeKind.Unspecified), Zone);

            Assert.False(Zone.IsInvalidTime(backInZone));
            Assert.True(backInZone > s_springForwardGap);
        }
    }
}
