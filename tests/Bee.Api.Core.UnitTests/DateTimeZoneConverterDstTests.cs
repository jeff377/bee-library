using System.ComponentModel;
using System.Data;
using Bee.Api.Core.JsonRpc;
using Bee.Base.Data;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// <see cref="DateTimeZoneConverter"/> 在觀測 DST 的時區下的行為。既有測試只用
    /// <c>Asia/Taipei</c> 與 <c>Pacific/Kiritimati</c>，兩者都是固定偏移、不觀測 DST，
    /// 因此兩個 DST 邊界（spring-forward 缺口、fall-back 重疊）完全沒有覆蓋。
    /// </summary>
    /// <remarks>
    /// 測試不寫死轉換日期是否為邊界，而是先以 <c>TimeZoneInfo.IsInvalidTime</c> /
    /// <c>TimeZoneInfo.IsAmbiguousTime</c> 斷言前提成立——若 runtime 的 tzdata 與
    /// 預期不同，測試會在前提處明確失敗，而不是以錯誤的理由通過。
    /// </remarks>
    public class DateTimeZoneConverterDstTests
    {
        private const string NewYork = "America/New_York";

        private static TimeZoneInfo Zone => TimeZoneInfo.FindSystemTimeZoneById(NewYork);

        // 2026 美國 DST：3/8 02:00 前進（02:00–02:59 不存在）、11/1 02:00 後退（01:00–01:59 出現兩次）。
        private static readonly DateTime s_springForwardGap = new(2026, 3, 8, 2, 30, 0, DateTimeKind.Unspecified);
        private static readonly DateTime s_fallBackAmbiguous = new(2026, 11, 1, 1, 30, 0, DateTimeKind.Unspecified);

        // 回撥重疊時段的兩個 UTC 值：01:30 EDT 與 01:30 EST，牆上時間相同。
        private static readonly DateTime s_firstOccurrenceUtc = new(2026, 11, 1, 5, 30, 0, DateTimeKind.Unspecified);

        /// <summary>
        /// 伺服端回應的形狀：UTC、Unchanged，另有一個可供「改別的欄位」的文字欄。
        /// </summary>
        private static DataTable BuildLoadedTable(DateTime utc)
        {
            var table = new DataTable("events");
            table.AddColumn("occurred_at", FieldDbType.DateTime);
            table.AddColumn("remark", FieldDbType.String);
            table.Rows.Add(utc, "a");
            table.AcceptChanges();
            return table;
        }

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
        [DisplayName("前提：2026-11-01 01:30 在 America/New_York 確實是重複出現的時刻")]
        public void Precondition_FallBackIsAmbiguous()
        {
            Assert.True(Zone.IsAmbiguousTime(s_fallBackAmbiguous));
        }

        [Fact]
        [DisplayName("UserToUtc 對 fall-back 重疊時刻應解析為標準時間，不擲例外")]
        public void UserToUtc_AmbiguousLocalTime_ResolvesToStandardOffset()
        {
            var converted = DateTimeZoneConverter.UserToUtc(BuildTableWithInstant(s_fallBackAmbiguous), NewYork);

            Assert.NotNull(converted);
            var expected = TimeZoneInfo.ConvertTimeToUtc(s_fallBackAmbiguous, Zone);
            Assert.Equal(DateTime.SpecifyKind(expected, DateTimeKind.Unspecified),
                (DateTime)converted.Rows[0]["occurred_at"]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [DisplayName("重疊時刻讀進後只改別的欄位，送出時兩個版本都應是原本的 UTC 值")]
        public void RoundTrip_AmbiguousInstantUntouched_SendsOriginalUtcBack(int hoursAfterFirstOccurrence)
        {
            var utc = s_firstOccurrenceUtc.AddHours(hoursAfterFirstOccurrence);
            var onScreen = DateTimeZoneConverter.UtcToUser(BuildLoadedTable(utc), NewYork)!;
            onScreen.Rows[0]["remark"] = "edited";

            var sent = DateTimeZoneConverter.UserToUtc(onScreen, NewYork)!;

            var row = sent.Rows[0];
            Assert.Equal(DataRowState.Modified, row.RowState);
            Assert.Equal(utc, (DateTime)row["occurred_at", DataRowVersion.Current]);
            Assert.Equal(utc, (DateTime)row["occurred_at", DataRowVersion.Original]);
        }

        [Fact]
        [DisplayName("使用者把重疊時刻改成別的牆上時間時，Current 依一般規則換算，Original 仍是原本的 UTC 值")]
        public void RoundTrip_AmbiguousInstantEdited_ConvertsTheNewValue()
        {
            var onScreen = DateTimeZoneConverter.UtcToUser(BuildLoadedTable(s_firstOccurrenceUtc), NewYork)!;
            var picked = new DateTime(2026, 11, 1, 1, 45, 0, DateTimeKind.Unspecified);
            onScreen.Rows[0]["occurred_at"] = picked;

            var sent = DateTimeZoneConverter.UserToUtc(onScreen, NewYork)!;

            var expected = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(picked, Zone), DateTimeKind.Unspecified);
            Assert.Equal(expected, (DateTime)sent.Rows[0]["occurred_at", DataRowVersion.Current]);
            Assert.Equal(s_firstOccurrenceUtc, (DateTime)sent.Rows[0]["occurred_at", DataRowVersion.Original]);
        }

        [Fact]
        [DisplayName("刪除讀進的列時，Original 應送回原本的 UTC 值（稽核讀它）")]
        public void RoundTrip_AmbiguousInstantOnDeletedRow_SendsOriginalUtcBack()
        {
            var onScreen = DateTimeZoneConverter.UtcToUser(BuildLoadedTable(s_firstOccurrenceUtc), NewYork)!;
            onScreen.Rows[0].Delete();

            var sent = DateTimeZoneConverter.UserToUtc(onScreen, NewYork)!;

            Assert.Equal(DataRowState.Deleted, sent.Rows[0].RowState);
            Assert.Equal(s_firstOccurrenceUtc, (DateTime)sent.Rows[0]["occurred_at", DataRowVersion.Original]);
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
        [DisplayName("UserToUtc 對 spring-forward 缺口內的時刻應前推一個 DST 差，不擲例外")]
        public void UserToUtc_InvalidLocalTime_SkipsGapForward()
        {
            var converted = DateTimeZoneConverter.UserToUtc(BuildTableWithInstant(s_springForwardGap), NewYork);

            Assert.NotNull(converted);

            // 02:30 不存在 → 前推該次轉換的 delta（此區為 1 小時）後的 03:30 才是真實時刻。
            var gap = Zone.GetUtcOffset(s_springForwardGap.Date.AddDays(1))
                      - Zone.GetUtcOffset(s_springForwardGap.Date.AddDays(-1));
            var expected = DateTime.SpecifyKind(
                TimeZoneInfo.ConvertTimeToUtc(s_springForwardGap.Add(gap), Zone), DateTimeKind.Unspecified);

            Assert.Equal(expected, (DateTime)converted.Rows[0]["occurred_at"]);
        }

        [Fact]
        [DisplayName("前推後的時刻轉回使用者時區應落在缺口之後，且是存在的時刻")]
        public void UserToUtc_InvalidLocalTime_ResultRoundTripsToARealTime()
        {
            var converted = DateTimeZoneConverter.UserToUtc(BuildTableWithInstant(s_springForwardGap), NewYork);
            Assert.NotNull(converted);

            var utc = (DateTime)converted.Rows[0]["occurred_at"];
            var backInZone = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utc, DateTimeKind.Unspecified), Zone);

            Assert.False(Zone.IsInvalidTime(backInZone));
            Assert.True(backInZone > s_springForwardGap);
        }
    }
}
