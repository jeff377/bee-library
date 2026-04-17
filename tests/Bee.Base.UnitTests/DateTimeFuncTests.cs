using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    public class DateTimeFuncTests
    {
        [Theory]
        [InlineData("1752-12-31", true)]
        [InlineData("1753-01-01", false)]
        [InlineData("2026-04-17", false)]
        [DisplayName("IsEmpty 應以 1753/1/1 作為有效下界")]
        public void IsEmpty_UsesSqlMinDateAsBoundary(string isoDate, bool expected)
        {
            var date = DateTime.Parse(isoDate, System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(expected, DateTimeFunc.IsEmpty(date));
        }

        [Fact]
        [DisplayName("IsEmpty 針對 DateTime.MinValue 應回傳 true")]
        public void IsEmpty_MinValue_ReturnsTrue()
        {
            Assert.True(DateTimeFunc.IsEmpty(DateTime.MinValue));
        }

        [Fact]
        [DisplayName("IsDate 針對 DateTime 物件與可剖析字串應回傳 true")]
        public void IsDate_AcceptsDateTimeAndParseableString()
        {
            Assert.True(DateTimeFunc.IsDate(DateTime.Now));
            Assert.True(DateTimeFunc.IsDate("2026-04-17"));
            Assert.True(DateTimeFunc.IsDate("2026-04-17 12:30:45"));
        }

        [Theory]
        [InlineData("not-a-date")]
        [InlineData("")]
        [InlineData("abc 2026")]
        [DisplayName("IsDate 對無法剖析的字串應回傳 false")]
        public void IsDate_InvalidString_ReturnsFalse(string input)
        {
            Assert.False(DateTimeFunc.IsDate(input));
        }

        [Fact]
        [DisplayName("Format 應以 InvariantCulture 套用格式")]
        public void Format_UsesInvariantCulture()
        {
            var date = new DateTime(2026, 4, 17, 9, 30, 15, DateTimeKind.Unspecified);
            Assert.Equal("2026-04-17", DateTimeFunc.Format(date, "yyyy-MM-dd"));
            Assert.Equal("2026-04-17 09:30:15", DateTimeFunc.Format(date, "yyyy-MM-dd HH:mm:ss"));
        }

        [Fact]
        [DisplayName("GetYearMonth 應回傳當月第一天且時間為 00:00:00")]
        public void GetYearMonth_ReturnsFirstOfMonth()
        {
            var input = new DateTime(2026, 4, 17, 9, 30, 15, DateTimeKind.Unspecified);
            var result = DateTimeFunc.GetYearMonth(input);

            Assert.Equal(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Unspecified), result);
        }
    }
}
