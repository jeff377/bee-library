using System.ComponentModel;
using System.Data;
using Bee.Base.Data;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// Covers the value-level behaviour of <see cref="FieldDbType.Time"/>: a time of day is carried
    /// as a fixed-width <c>"HH:mm"</c> string, normalised on the way in, and has the empty string —
    /// never <c>"00:00"</c> — as its unset value (ADR-033).
    /// </summary>
    public class TimeOfDayValueTests
    {
        private static readonly string[] s_unsortedTimes = ["9:05", "23:59", "08:30", "0:00"];
        private static readonly string[] s_sortedTimes = ["00:00", "08:30", "09:05", "23:59"];

        [Fact]
        [DisplayName("Time 的 CLR 型別為 string，DbType 為 String")]
        public void Time_MapsToStringTypes()
        {
            Assert.Equal(typeof(string), DbTypeConverter.ToType(FieldDbType.Time));
            Assert.Equal(DbType.String, DbTypeConverter.ToDbType(FieldDbType.Time));
        }

        [Fact]
        [DisplayName("Time 必須位於列舉尾端，避免既有 wire payload 位移")]
        public void Time_IsAppendedAtEndOfEnum()
        {
            var values = Enum.GetValues<FieldDbType>();
            Assert.Equal(FieldDbType.Time, values[^1]);
        }

        [Fact]
        [DisplayName("Time 的預設值為空字串，不是 00:00")]
        public void GetDefaultValue_Time_ReturnsEmptyString()
        {
            // Midnight is a legal time of day, so it cannot double as "unset".
            Assert.Equal(string.Empty, FieldDbType.Time.GetDefaultValue());
        }

        [Theory]
        [InlineData("08:30", "08:30")]
        [InlineData("8:30", "08:30")]
        [InlineData("  8:30  ", "08:30")]
        [InlineData("00:00", "00:00")]
        [InlineData("23:59", "23:59")]
        [InlineData("", "")]
        [InlineData("25:99", "")]
        [InlineData("abc", "")]
        [DisplayName("ToFieldValue 應將時刻正規化為定寬 HH:mm")]
        public void ToFieldValue_Time_NormalizesToFixedWidth(string input, string expected)
        {
            Assert.Equal(expected, FieldDbType.Time.ToFieldValue(input));
        }

        [Fact]
        [DisplayName("正規化後的定寬字串，字典序即時序")]
        public void NormalizedValues_SortChronologically()
        {
            var raw = s_unsortedTimes;
            var normalized = raw.Select(v => (string)FieldDbType.Time.ToFieldValue(v)).ToList();
            var sorted = normalized.OrderBy(v => v, StringComparer.Ordinal).ToList();
            Assert.Equal(s_sortedTimes, sorted);
        }

        [Theory]
        [InlineData("08:30", 8, 30)]
        [InlineData("8:30", 8, 30)]
        [InlineData("00:00", 0, 0)]
        [InlineData("23:59", 23, 59)]
        [DisplayName("CTimeOnly 應解析合法時刻字串")]
        public void CTimeOnly_ValidText_ReturnsTimeOnly(string input, int hour, int minute)
        {
            Assert.Equal(new TimeOnly(hour, minute), ValueUtilities.CTimeOnly(input));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("25:00")]
        [InlineData("08:99")]
        [InlineData("8")]
        [InlineData("08:30:15")]
        [InlineData("abc")]
        [DisplayName("CTimeOnly 於未填或格式不合時應回傳 null")]
        public void CTimeOnly_UnsetOrMalformed_ReturnsNull(string input)
        {
            Assert.Null(ValueUtilities.CTimeOnly(input));
        }

        [Fact]
        [DisplayName("CTimeOnly 對 null 與 DBNull 應回傳 null")]
        public void CTimeOnly_NullLike_ReturnsNull()
        {
            Assert.Null(ValueUtilities.CTimeOnly(null));
            Assert.Null(ValueUtilities.CTimeOnly(DBNull.Value));
        }

        [Fact]
        [DisplayName("CTimeOnly 應接受 TimeOnly / DateTime / 合法範圍的 TimeSpan")]
        public void CTimeOnly_TemporalSources_Accepted()
        {
            Assert.Equal(new TimeOnly(8, 30), ValueUtilities.CTimeOnly(new TimeOnly(8, 30)));
            Assert.Equal(new TimeOnly(8, 30),
                ValueUtilities.CTimeOnly(new DateTime(2026, 7, 27, 8, 30, 0, DateTimeKind.Unspecified)));
            Assert.Equal(new TimeOnly(8, 30), ValueUtilities.CTimeOnly(new TimeSpan(8, 30, 0)));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(24)]
        [InlineData(30)]
        [DisplayName("CTimeOnly 應拒絕超出一日範圍的 TimeSpan")]
        public void CTimeOnly_OutOfRangeTimeSpan_ReturnsNull(int hours)
        {
            // A TimeSpan is a duration and can hold values a time of day cannot; this method is the
            // gate that keeps them out of the framework.
            Assert.Null(ValueUtilities.CTimeOnly(TimeSpan.FromHours(hours)));
        }

        [Fact]
        [DisplayName("CTimeString 應輸出定寬 HH:mm，未填則為空字串")]
        public void CTimeString_NormalizesOrReturnsEmpty()
        {
            Assert.Equal("08:30", ValueUtilities.CTimeString("8:30"));
            Assert.Equal("08:30", ValueUtilities.CTimeString(new TimeOnly(8, 30)));
            Assert.Equal(string.Empty, ValueUtilities.CTimeString(null));
            Assert.Equal(string.Empty, ValueUtilities.CTimeString("25:00"));
        }

        [Fact]
        [DisplayName("TimeOnlyLength 應與 TimeOnlyFormat 一致")]
        public void TimeOnlyLength_MatchesFormat()
        {
            Assert.Equal(5, ValueUtilities.TimeOnlyLength);
            Assert.Equal(ValueUtilities.TimeOnlyFormat.Length, ValueUtilities.TimeOnlyLength);
        }

        [Fact]
        [DisplayName("AddColumn(Time) 應建立 string 欄並保留 Time 標記")]
        public void AddColumn_Time_IsStringColumnCarryingTheMarker()
        {
            var table = new DataTable("probe");
            var column = table.AddColumn("work_start", FieldDbType.Time);

            Assert.Equal(typeof(string), column.DataType);
            // Without the marker the column would read back as a plain String on the wire, which is
            // exactly the self-description this type exists to provide.
            Assert.Equal(FieldDbType.Time, column.ResolveFieldDbType());
        }
    }
}
