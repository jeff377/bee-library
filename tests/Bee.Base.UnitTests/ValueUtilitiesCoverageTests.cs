using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// ValueUtilities 覆蓋率補強測試：針對 IsEmpty(DateTime) 的邊界、CStr 的 enum/ToString 分支，
    /// 以及 CInt / CDecimal 的 OverflowException catch 分支（值超出目標型別範圍時回傳 defaultValue）。
    /// </summary>
    public class ValueUtilitiesCoverageTests
    {
        // ---- IsEmpty(DateTime) 邊界（line 76）----

        [Theory]
        [InlineData(1752, 12, 31, true)]  // 1753 之前 → empty
        [InlineData(1753, 1, 1, false)]   // 邊界值 → 非 empty
        [InlineData(2026, 7, 11, false)]  // 一般日期 → 非 empty
        [DisplayName("IsEmpty(DateTime) 對 1753 前回傳 true、1753 起回傳 false")]
        public void IsEmpty_DateTimeBoundary_ReturnsExpectedResult(int y, int m, int d, bool expected)
        {
            var value = new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Unspecified);
            Assert.Equal(expected, ValueUtilities.IsEmpty(value));
        }

        [Fact]
        [DisplayName("IsEmpty(DateTime) 對 MinValue 回傳 true")]
        public void IsEmpty_DateTimeMinValue_ReturnsTrue()
        {
            Assert.True(ValueUtilities.IsEmpty(DateTime.MinValue));
        }

        // ---- CStr enum / ToString 分支（line 129 / 131）----

        [Fact]
        [DisplayName("CStr 對 enum 回傳其名稱，對一般物件回傳 ToString 結果")]
        public void CStr_EnumAndObject_ReturnsExpectedString()
        {
            // enum 分支（line 129）
            Assert.Equal("Hour", ValueUtilities.CStr(DateInterval.Hour));
            // ToString 分支（line 131）
            Assert.Equal("42", ValueUtilities.CStr(42));
            Assert.Equal("3.5", ValueUtilities.CStr(3.5));
        }

        // ---- CInt OverflowException catch（line 309/311）----

        [Fact]
        [DisplayName("CInt 對超出 int 範圍的值應回傳 defaultValue（OverflowException 分支）")]
        public void CInt_ValueOverflowsInt_ReturnsDefault()
        {
            // 1e20 為 double，Convert.ToInt32 會拋 OverflowException → catch → defaultValue
            Assert.Equal(0, ValueUtilities.CInt(1e20));
            Assert.Equal(-9, ValueUtilities.CInt(1e20, -9));
        }

        // ---- CDecimal OverflowException catch（line 363/365）----

        [Fact]
        [DisplayName("CDecimal 對超出 decimal 範圍的值應回傳 defaultValue（OverflowException 分支）")]
        public void CDecimal_ValueOverflowsDecimal_ReturnsDefault()
        {
            // 1e30 超過 decimal 上限（約 7.9e28），Convert.ToDecimal 拋 OverflowException → catch
            Assert.Equal(0m, ValueUtilities.CDecimal(1e30));
            Assert.Equal(-1m, ValueUtilities.CDecimal(1e30, -1m));
        }

        // ---- CDouble / CDecimal 正常轉換主體（line 326 / 353）----

        [Fact]
        [DisplayName("CDouble 對有效數值輸入應回傳對應 double")]
        public void CDouble_ValidInput_ReturnsConvertedValue()
        {
            Assert.Equal(123.45d, ValueUtilities.CDouble("123.45"));
            Assert.Equal(7d, ValueUtilities.CDouble(7));
            Assert.Equal(1d, ValueUtilities.CDouble(true));
        }

        [Fact]
        [DisplayName("CDecimal 對有效數值輸入應回傳對應 decimal")]
        public void CDecimal_ValidInput_ReturnsConvertedValue()
        {
            Assert.Equal(123.45m, ValueUtilities.CDecimal("123.45"));
            Assert.Equal(7m, ValueUtilities.CDecimal(7));
            Assert.Equal(1m, ValueUtilities.CDecimal(true));
        }
    }
}
