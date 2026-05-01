using System.Collections;
using System.ComponentModel;
using Bee.Base.Data;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// Tests for <see cref="ValueUtilities"/> covering value-emptiness checks
    /// (<c>IsEmpty</c> overloads / <c>IsNullOrDBNull</c>) and the framework's
    /// <c>Cxxx</c> type-conversion API. The framework encapsulates
    /// <see cref="System.Globalization.CultureInfo.InvariantCulture"/> and
    /// numeric parsing defaults so call sites do not pass them.
    /// </summary>
    public class ValueUtilitiesTests
    {
        private static readonly int[] s_singleIntArray = { 1 };

        // ---- IsNullOrDBNull / IsEmpty ----

        [Fact]
        [DisplayName("IsNullOrDBNull 對 null 與 DBNull 回傳 true,對有效值回傳 false")]
        public void IsNullOrDBNull_VariousValues_ReturnsExpectedResult()
        {
            Assert.True(ValueUtilities.IsNullOrDBNull(null));
            Assert.True(ValueUtilities.IsNullOrDBNull(DBNull.Value));
            Assert.False(ValueUtilities.IsNullOrDBNull("value"));
            Assert.False(ValueUtilities.IsNullOrDBNull(0));
            Assert.False(ValueUtilities.IsNullOrDBNull(new object()));
        }

        [Fact]
        [DisplayName("IsEmpty(object) 對各種型別應走對應分支")]
        public void IsEmpty_Object_DispatchesToCorrectOverload()
        {
            Assert.True(ValueUtilities.IsEmpty((object)null!));
            Assert.True(ValueUtilities.IsEmpty((object)DBNull.Value));
            Assert.True(ValueUtilities.IsEmpty((object)string.Empty));
            Assert.True(ValueUtilities.IsEmpty((object)"   "));
            Assert.True(ValueUtilities.IsEmpty((object)Guid.Empty));
            Assert.True(ValueUtilities.IsEmpty((object)new List<int>()));
            Assert.True(ValueUtilities.IsEmpty((object)DateTime.MinValue));

            Assert.False(ValueUtilities.IsEmpty((object)"abc"));
            Assert.False(ValueUtilities.IsEmpty((object)Guid.NewGuid()));
            Assert.False(ValueUtilities.IsEmpty((object)new List<int> { 1 }));
            Assert.False(ValueUtilities.IsEmpty((object)new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.False(ValueUtilities.IsEmpty((object)123));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("   ", true)]
        [InlineData("abc", false)]
        [DisplayName("IsEmpty(string) 對 null/空字串/空白回傳 true")]
        public void IsEmpty_String_ReturnsExpectedResult(string? value, bool expected)
        {
            Assert.Equal(expected, ValueUtilities.IsEmpty(value!));
        }

        [Fact]
        [DisplayName("IsEmpty(Guid) 對 Guid.Empty 回傳 true,對其他值回傳 false")]
        public void IsEmpty_Guid_ReturnsExpectedResult()
        {
            Assert.True(ValueUtilities.IsEmpty(Guid.Empty));
            Assert.False(ValueUtilities.IsEmpty(Guid.NewGuid()));
        }

        [Fact]
        [DisplayName("IsEmpty(DateTime) 對 MinValue/1753 之前回傳 true,對正常日期回傳 false")]
        public void IsEmpty_DateTime_ReturnsExpectedResult()
        {
            Assert.True(ValueUtilities.IsEmpty(DateTime.MinValue));
            Assert.True(ValueUtilities.IsEmpty(new DateTime(1752, 12, 31, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.False(ValueUtilities.IsEmpty(new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.False(ValueUtilities.IsEmpty(new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Unspecified)));
        }

        [Fact]
        [DisplayName("IsEmpty(IList) 對 null 與空集合回傳 true,對有元素集合回傳 false")]
        public void IsEmpty_IList_ReturnsExpectedResult()
        {
            Assert.True(ValueUtilities.IsEmpty((IList)null!));
            Assert.True(ValueUtilities.IsEmpty((IList)new List<int>()));
            Assert.False(ValueUtilities.IsEmpty((IList)new List<int> { 1, 2 }));
        }

        [Fact]
        [DisplayName("IsEmpty(IEnumerable) 對 null 與無元素回傳 true,對有元素回傳 false")]
        public void IsEmpty_IEnumerable_ReturnsExpectedResult()
        {
            Assert.True(ValueUtilities.IsEmpty((IEnumerable)null!));
            Assert.True(ValueUtilities.IsEmpty((IEnumerable)Array.Empty<int>()));
            Assert.False(ValueUtilities.IsEmpty((IEnumerable)s_singleIntArray));
        }

        [Fact]
        [DisplayName("IsEmpty(byte[]) 對 null 與空陣列回傳 true,對有內容陣列回傳 false")]
        public void IsEmpty_ByteArray_ReturnsExpectedResult()
        {
            Assert.True(ValueUtilities.IsEmpty((byte[])null!));
            Assert.True(ValueUtilities.IsEmpty(Array.Empty<byte>()));
            Assert.False(ValueUtilities.IsEmpty(new byte[] { 0x01 }));
        }

        // ---- CStr ----

        [Fact]
        [DisplayName("CStr(object) 對各種輸入回傳對應字串表示")]
        public void CStr_Object_ReturnsExpectedString()
        {
            Assert.Equal(string.Empty, ValueUtilities.CStr(null!));
            Assert.Equal(string.Empty, ValueUtilities.CStr(DBNull.Value));
            Assert.Equal("abc", ValueUtilities.CStr("abc"));
            Assert.Equal("Day", ValueUtilities.CStr(DateInterval.Day));
            Assert.Equal("123", ValueUtilities.CStr(123));
        }

        [Fact]
        [DisplayName("CStr(object, defaultValue) 對 null/DBNull 回傳 defaultValue,對非 null 回傳字串")]
        public void CStr_ObjectWithDefault_ReturnsExpectedString()
        {
            Assert.Equal("N/A", ValueUtilities.CStr(null!, "N/A"));
            Assert.Equal("N/A", ValueUtilities.CStr(DBNull.Value, "N/A"));
            Assert.Equal("abc", ValueUtilities.CStr("abc", "N/A"));
            Assert.Equal("Day", ValueUtilities.CStr(DateInterval.Day, "N/A"));
        }

        // ---- CBool ----

        [Theory]
        [InlineData("1", true)]
        [InlineData("T", true)]
        [InlineData("TRUE", true)]
        [InlineData("true", true)]
        [InlineData("Y", true)]
        [InlineData("YES", true)]
        [InlineData("是", true)]
        [InlineData("真", true)]
        [InlineData("0", false)]
        [InlineData("N", false)]
        [InlineData("false", false)]
        [InlineData("other", false)]
        [DisplayName("CBool(string) 對常見真假值字串回傳對應布林值")]
        public void CBool_String_ReturnsExpectedResult(string value, bool expected)
        {
            Assert.Equal(expected, ValueUtilities.CBool(value));
        }

        [Fact]
        [DisplayName("CBool(string) 對空字串回傳 defaultValue")]
        public void CBool_String_Empty_ReturnsDefault()
        {
            Assert.False(ValueUtilities.CBool(""));
            Assert.True(ValueUtilities.CBool("", true));
            Assert.True(ValueUtilities.CBool(null!, true));
        }

        [Fact]
        [DisplayName("CBool(object) 對 bool 型別直接轉型,對其他型別透過字串轉換")]
        public void CBool_Object_ReturnsExpectedResult()
        {
            Assert.True(ValueUtilities.CBool((object)true));
            Assert.False(ValueUtilities.CBool((object)false));
            Assert.True(ValueUtilities.CBool((object)"1"));
            Assert.False(ValueUtilities.CBool((object)"0"));
            Assert.False(ValueUtilities.CBool((object)null!));
            Assert.True(ValueUtilities.CBool((object)null!, true));
        }

        // ---- CEnum ----

        [Theory]
        [InlineData("Day", DateInterval.Day)]
        [InlineData("day", DateInterval.Day)] // 框架預設不區分大小寫
        [InlineData("Hour", DateInterval.Hour)]
        [DisplayName("CEnum(string, Type) 對合法字串回傳對應 enum 值(不區分大小寫)")]
        public void CEnum_ValidString_ReturnsEnumValue(string input, DateInterval expected)
        {
            // 本測試刻意呼叫 non-generic overload,驗證其行為
#pragma warning disable CA2263 // Prefer generic overload when type is known
            var result = ValueUtilities.CEnum(input, typeof(DateInterval));
#pragma warning restore CA2263
            Assert.Equal(expected, (DateInterval)result);
        }

        [Fact]
        [DisplayName("CEnum<T>(string) 對合法字串回傳 enum 值,對非法字串拋出 ArgumentException")]
        public void CEnum_Generic_ValidAndInvalid_BehavesAsExpected()
        {
            Assert.Equal(DateInterval.Day, ValueUtilities.CEnum<DateInterval>("Day"));
            Assert.Throws<ArgumentException>(() => ValueUtilities.CEnum<DateInterval>("NotExist"));
        }

        // ---- IsNumeric / ConvertToNumber ----

        [Fact]
        [DisplayName("IsNumeric 應正確判斷各種型別的數值性")]
        public void IsNumeric_VariousTypes_ReturnsExpectedResult()
        {
            // 布林值
            Assert.True(ValueUtilities.IsNumeric(true));
            Assert.True(ValueUtilities.IsNumeric(false));

            // Enum
            Assert.True(ValueUtilities.IsNumeric(DateInterval.Day));
            Assert.True(ValueUtilities.IsNumeric(DateInterval.Hour));

            // 數值型別
            Assert.True(ValueUtilities.IsNumeric(123));
            Assert.True(ValueUtilities.IsNumeric(123.45));
            Assert.True(ValueUtilities.IsNumeric(123.45m));

            // 字串
            Assert.True(ValueUtilities.IsNumeric("123"));
            Assert.True(ValueUtilities.IsNumeric("123.45"));
            Assert.False(ValueUtilities.IsNumeric("abc"));

            // 特殊值
            Assert.False(ValueUtilities.IsNumeric(null!));
            Assert.False(ValueUtilities.IsNumeric(new object()));
            Assert.False(ValueUtilities.IsNumeric(DateTime.Now));
        }

        [Theory]
        [InlineData("12345", 5, true)]
        [InlineData("12345", 4, false)]
        [InlineData("abc", 3, false)]
        [InlineData("", 0, false)]
        [DisplayName("IsNumeric(string, length) 應同時檢查數值性與長度")]
        public void IsNumeric_WithLength_ChecksBothConditions(string value, int length, bool expected)
        {
            Assert.Equal(expected, ValueUtilities.IsNumeric(value, length));
        }

        [Fact]
        [DisplayName("ConvertToNumber 對各種輸入回傳對應數值")]
        public void ConvertToNumber_VariousInputs_ReturnsExpectedResult()
        {
            Assert.Equal(0, ValueUtilities.ConvertToNumber(null!));
            Assert.Equal(0, ValueUtilities.ConvertToNumber(DBNull.Value));
            Assert.Equal(0, ValueUtilities.ConvertToNumber(""));

            Assert.Equal((double)123.45, ValueUtilities.ConvertToNumber("123.45"));
            Assert.Equal(1, ValueUtilities.ConvertToNumber(true));
            Assert.Equal(0, ValueUtilities.ConvertToNumber(false));
            Assert.Equal((int)DateInterval.Day, ValueUtilities.ConvertToNumber(DateInterval.Day));
            Assert.Equal(123, ValueUtilities.ConvertToNumber(123));
            Assert.Equal(123.45m, ValueUtilities.ConvertToNumber(123.45m));
        }

        [Fact]
        [DisplayName("ConvertToNumber 對無法轉換的值拋出 InvalidCastException")]
        public void ConvertToNumber_InvalidInput_Throws()
        {
            Assert.Throws<InvalidCastException>(() => ValueUtilities.ConvertToNumber(new object()));
        }

        private sealed class NumericToString
        {
            public override string ToString() => "3.14";
        }

        [Fact]
        [DisplayName("ConvertToNumber 對非支援型別但 ToString 為數值應回傳 double")]
        public void ConvertToNumber_NonStandardType_FallsBackToToString()
        {
            // 進入最後 double.TryParse(value.ToString(), ...) 分支
            var value = new NumericToString();
            var result = ValueUtilities.ConvertToNumber(value);
            Assert.Equal(3.14d, result);
        }

        // ---- CInt / CDouble / CDecimal ----

        [Theory]
        [InlineData(null, 0)]
        [InlineData("", 0)]
        [InlineData("123", 123)]
        [InlineData("  ", 0)]
        [InlineData(123, 123)]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        [InlineData("abc", 0)] // 無法轉換回傳 defaultValue
        [DisplayName("CInt 對各種輸入回傳對應整數,無法轉換時回傳 defaultValue")]
        public void CInt_VariousInputs_ReturnsExpectedResult(object? value, int expected)
        {
            Assert.Equal(expected, ValueUtilities.CInt(value!));
        }

        [Fact]
        [DisplayName("CInt 對 Enum 回傳對應整數")]
        public void CInt_Enum_ReturnsIntegerValue()
        {
            Assert.Equal((int)DateInterval.Day, ValueUtilities.CInt(DateInterval.Day));
        }

        [Fact]
        [DisplayName("CDouble 對各種輸入回傳對應 double,無法轉換時回傳 defaultValue")]
        public void CDouble_VariousInputs_ReturnsExpectedResult()
        {
            Assert.Equal(0d, ValueUtilities.CDouble(null!));
            Assert.Equal(0d, ValueUtilities.CDouble(DBNull.Value));
            Assert.Equal(123.45, ValueUtilities.CDouble("123.45"));
            Assert.Equal(1d, ValueUtilities.CDouble(true));
            Assert.Equal(-1.5d, ValueUtilities.CDouble("abc", -1.5));
        }

        [Fact]
        [DisplayName("CDecimal 對各種輸入回傳對應 decimal,無法轉換時回傳 defaultValue")]
        public void CDecimal_VariousInputs_ReturnsExpectedResult()
        {
            Assert.Equal(0m, ValueUtilities.CDecimal(null!));
            Assert.Equal(0m, ValueUtilities.CDecimal(DBNull.Value));
            Assert.Equal(123.45m, ValueUtilities.CDecimal("123.45"));
            Assert.Equal(1m, ValueUtilities.CDecimal(true));
            Assert.Equal(-1.5m, ValueUtilities.CDecimal("abc", -1.5m));
        }

        // ---- CDateTime / CDate ----

        [Fact]
        [DisplayName("CDateTime 對各種輸入回傳對應 DateTime")]
        public void CDateTime_VariousInputs_ReturnsExpectedResult()
        {
            Assert.Equal(default, ValueUtilities.CDateTime(null!));
            Assert.Equal(default, ValueUtilities.CDateTime(DBNull.Value));
            Assert.Equal(default, ValueUtilities.CDateTime(""));

            var expected = new DateTime(2015, 3, 12, 0, 0, 0, DateTimeKind.Unspecified);
            Assert.Equal(expected, ValueUtilities.CDateTime(expected));
            Assert.Equal(expected, ValueUtilities.CDateTime("2015-03-12"));
            Assert.Equal(expected, ValueUtilities.CDateTime("20150312"));

            // ROC date(民國年)
            Assert.Equal(expected, ValueUtilities.CDateTime("1040312"));

            // 非數值字串 → StrToDate 回傳 DateTime.MinValue(不丟例外)
            Assert.Equal(DateTime.MinValue, ValueUtilities.CDateTime("not-a-date"));
        }

        [Theory]
        [InlineData("20150312", 2015, 3, 12)] // 8-digit 西元
        [InlineData("1040312", 2015, 3, 12)]  // 7-digit 民國
        [InlineData("201503", 2015, 3, 1)]    // 6-digit 西元年月
        [InlineData("10403", 2015, 3, 1)]     // 5-digit 民國年月
        [InlineData("2015", 2015, 1, 1)]      // 4-digit 西元年
        [InlineData("104", 2015, 1, 1)]       // 3-digit 民國年
        [DisplayName("CDateTime 應依字串長度解析各種日期格式")]
        public void CDateTime_VariousLengths_ParsesCorrectly(string input, int y, int m, int d)
        {
            var result = ValueUtilities.CDateTime(input);
            Assert.Equal(new DateTime(y, m, d), result);
        }

        [Fact]
        [DisplayName("CDateTime 對未支援長度的數字字串應回傳 DateTime.MinValue")]
        public void CDateTime_UnsupportedLength_ReturnsMinValue()
        {
            // 長度 2 不在 switch 的 3/4/5/6/7/8 範圍內 → default 分支 → MinValue
            var result = ValueUtilities.CDateTime("12");
            Assert.Equal(DateTime.MinValue, result);
        }

        [Fact]
        [DisplayName("CDateTime 對非數字字串經 StrToDate 返回 MinValue")]
        public void CDateTime_NonNumericString_ReturnsMinValue()
        {
            // "abcdefgh" 移除分隔字元後非數字 → StrToDate 直接回傳 MinValue
            var result = ValueUtilities.CDateTime("abcdefgh");
            Assert.Equal(DateTime.MinValue, result);
        }

        [Fact]
        [DisplayName("CDateTime 對無法轉成日期的數字字串應回傳 defaultValue")]
        public void CDateTime_InvalidCalendarDate_FallsBackToDefault()
        {
            // 20150230 → "2015-02-30" → Convert.ToDateTime 拋例外 → catch → defaultValue
            var fallback = new DateTime(2000, 1, 1);
            var result = ValueUtilities.CDateTime("20150230", fallback);
            Assert.Equal(fallback, result);
        }

        [Fact]
        [DisplayName("CDate 只保留日期部分")]
        public void CDate_ReturnsDatePortionOnly()
        {
            var input = new DateTime(2026, 4, 18, 15, 30, 45, DateTimeKind.Unspecified);
            var result = ValueUtilities.CDate(input);
            Assert.Equal(new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Unspecified), result);
        }

        [Fact]
        [DisplayName("CDate 應取得日期部分(時間為零)")]
        public void CDate_ReturnsDateOnly()
        {
            var result = ValueUtilities.CDate("20150312");
            Assert.Equal(new DateTime(2015, 3, 12), result);
            Assert.Equal(TimeSpan.Zero, result.TimeOfDay);
        }

        // ---- CGuid ----

        [Fact]
        [DisplayName("CGuid(string) 對合法 Guid 字串回傳對應值,對空字串回傳 Guid.Empty,對非法字串拋出例外")]
        public void CGuid_String_BehavesAsExpected()
        {
            var guid = Guid.NewGuid();
            Assert.Equal(guid, ValueUtilities.CGuid(guid.ToString()));
            Assert.Equal(Guid.Empty, ValueUtilities.CGuid(string.Empty));
            Assert.Equal(Guid.Empty, ValueUtilities.CGuid(null!));
            Assert.Throws<FormatException>(() => ValueUtilities.CGuid("not-a-guid"));
        }

        [Fact]
        [DisplayName("CGuid(object) 對 null/DBNull/非字串回傳 Guid.Empty,對 Guid/合法字串回傳對應值")]
        public void CGuid_Object_BehavesAsExpected()
        {
            var guid = Guid.NewGuid();
            Assert.Equal(guid, ValueUtilities.CGuid((object)guid));
            Assert.Equal(guid, ValueUtilities.CGuid((object)guid.ToString()));
            Assert.Equal(Guid.Empty, ValueUtilities.CGuid((object)null!));
            Assert.Equal(Guid.Empty, ValueUtilities.CGuid((object)DBNull.Value));
            Assert.Equal(Guid.Empty, ValueUtilities.CGuid((object)123));
        }

        // ---- CFieldValue / CDbFieldValue ----

        [Fact]
        [DisplayName("CFieldValue 依據 FieldDbType 走對應轉換分支")]
        public void CFieldValue_VariousDbTypes_ReturnsExpectedResult()
        {
            Assert.Equal("abc", ValueUtilities.CFieldValue(FieldDbType.String, "abc"));
            Assert.Equal("abc", ValueUtilities.CFieldValue(FieldDbType.Text, "abc"));
            Assert.True((bool)ValueUtilities.CFieldValue(FieldDbType.Boolean, "1")!);
            Assert.Equal(123, ValueUtilities.CFieldValue(FieldDbType.Integer, "123"));
            Assert.Equal(123.45m, ValueUtilities.CFieldValue(FieldDbType.Decimal, "123.45"));
            Assert.Equal(123.45m, ValueUtilities.CFieldValue(FieldDbType.Currency, "123.45"));

            var date = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Unspecified);
            Assert.Equal(date, ValueUtilities.CFieldValue(FieldDbType.Date, "2026-04-18"));
            Assert.Equal(date, ValueUtilities.CFieldValue(FieldDbType.DateTime, "2026-04-18"));

            var guid = Guid.NewGuid();
            Assert.Equal(guid, ValueUtilities.CFieldValue(FieldDbType.Guid, guid.ToString()));

            // 未涵蓋的 FieldDbType 應原樣回傳
            var raw = new byte[] { 0x01, 0x02 };
            Assert.Same(raw, ValueUtilities.CFieldValue(FieldDbType.Binary, raw));
        }

        [Fact]
        [DisplayName("CDbFieldValue 對 DateTime.MinValue 回傳 DBNull.Value,其餘走 CFieldValue")]
        public void CDbFieldValue_DateTimeMinValue_ReturnsDBNull()
        {
            Assert.Equal(DBNull.Value, ValueUtilities.CDbFieldValue(FieldDbType.DateTime, DateTime.MinValue));
            Assert.Equal(DBNull.Value, ValueUtilities.CDbFieldValue(FieldDbType.Date, DateTime.MinValue));

            // 一般日期走 CFieldValue
            var date = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Unspecified);
            Assert.Equal(date, ValueUtilities.CDbFieldValue(FieldDbType.DateTime, date));
            Assert.Equal("abc", ValueUtilities.CDbFieldValue(FieldDbType.String, "abc"));
        }
    }
}
