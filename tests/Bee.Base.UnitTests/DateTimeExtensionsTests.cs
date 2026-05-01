using System.ComponentModel;
using System.Globalization;

namespace Bee.Base.UnitTests
{
    public class DateTimeExtensionsTests
    {
        [Theory]
        [InlineData("1752-12-31", true)]
        [InlineData("1753-01-01", false)]
        [InlineData("2026-04-17", false)]
        [DisplayName("IsEmpty 應以 1753/1/1 作為有效下界")]
        public void IsEmpty_UsesSqlMinDateAsBoundary(string isoDate, bool expected)
        {
            var date = DateTime.Parse(isoDate, CultureInfo.InvariantCulture);
            Assert.Equal(expected, date.IsEmpty());
        }

        [Fact]
        [DisplayName("IsEmpty 針對 DateTime.MinValue 應回傳 true")]
        public void IsEmpty_MinValue_ReturnsTrue()
        {
            Assert.True(DateTime.MinValue.IsEmpty());
        }

        [Fact]
        [DisplayName("GetYearMonth 應回傳當月第一天且時間為 00:00:00")]
        public void GetYearMonth_ReturnsFirstOfMonth()
        {
            var input = new DateTime(2026, 4, 17, 9, 30, 15, DateTimeKind.Unspecified);
            var result = input.GetYearMonth();

            Assert.Equal(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Unspecified), result);
        }
    }
}
