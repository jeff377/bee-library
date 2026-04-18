using System.Collections;
using System.ComponentModel;
using Bee.Base.Data;
using Bee.Base.Serialization;

namespace Bee.Base.UnitTests
{
    public class BaseFuncTests
    {
        private static readonly int[] s_singleIntArray = { 1 };
        private static readonly string[] s_argsOnlyExe = { "app.exe" };
        private static readonly string[] s_argsWithPositional = { "app.exe", "positional", "-single", "another" };
        private static readonly string[] s_argsKeyValue = { "app.exe", "--name", "bee", "--count", "42" };
        private static readonly string[] s_argsFlagThenOption = { "app.exe", "--verbose", "--name", "bee" };
        private static readonly string[] s_argsFlagAtEnd = { "app.exe", "--flag" };
        private static readonly string[] s_argsCaseInsensitive = { "app.exe", "--Name", "bee" };

        #region 既有測試

        [Fact]
        [DisplayName("IsNumeric 應正確判斷各種型別的數值性")]
        public void IsNumeric_VariousTypes_ReturnsExpectedResult()
        {
            // 布林值測試
            Assert.True(BaseFunc.IsNumeric(true));
            Assert.True(BaseFunc.IsNumeric(false));

            // 列舉型別測試
            Assert.True(BaseFunc.IsNumeric(DateInterval.Day));
            Assert.True(BaseFunc.IsNumeric(DateInterval.Hour));

            // 數值型別測試
            Assert.True(BaseFunc.IsNumeric(123)); // 整數
            Assert.True(BaseFunc.IsNumeric(123.45)); // 浮點數
            Assert.True(BaseFunc.IsNumeric(123.45m)); // 十進位數

            // 字串型別測試
            Assert.True(BaseFunc.IsNumeric("123"));
            Assert.True(BaseFunc.IsNumeric("123.45"));
            Assert.False(BaseFunc.IsNumeric("abc"));

            // 特殊值測試
            Assert.False(BaseFunc.IsNumeric(null!));
            Assert.False(BaseFunc.IsNumeric(new object()));
            Assert.False(BaseFunc.IsNumeric(DateTime.Now));
        }

        [Fact]
        [DisplayName("RndInt 應回傳指定範圍內的隨機整數")]
        public void RndInt_ReturnsValueWithinRange()
        {
            int min = 1;
            int max = 10;

            for (int i = 0; i < 100; i++)
            {
                int value = BaseFunc.RndInt(min, max);
                Assert.InRange(value, min, max - 1);
            }
        }

        #endregion

        #region 空值判斷 (IsDBNull / IsNullOrDBNull / IsNullOrEmpty / IsEmpty)

        [Fact]
        [DisplayName("IsDBNull 對 DBNull 回傳 true，對一般物件回傳 false")]
        public void IsDBNull_VariousValues_ReturnsExpectedResult()
        {
            Assert.True(BaseFunc.IsDBNull(DBNull.Value));
            Assert.False(BaseFunc.IsDBNull("value"));
            Assert.False(BaseFunc.IsDBNull(123));
            Assert.False(BaseFunc.IsDBNull(new object()));
        }

        [Fact]
        [DisplayName("IsNullOrDBNull 對 null 與 DBNull 回傳 true，對有效值回傳 false")]
        public void IsNullOrDBNull_VariousValues_ReturnsExpectedResult()
        {
            Assert.True(BaseFunc.IsNullOrDBNull(null));
            Assert.True(BaseFunc.IsNullOrDBNull(DBNull.Value));
            Assert.False(BaseFunc.IsNullOrDBNull("value"));
            Assert.False(BaseFunc.IsNullOrDBNull(0));
            Assert.False(BaseFunc.IsNullOrDBNull(new object()));
        }

        [Fact]
        [DisplayName("IsNullOrEmpty(byte[]) 對 null 與空陣列回傳 true，對有內容陣列回傳 false")]
        public void IsNullOrEmpty_ByteArray_ReturnsExpectedResult()
        {
            Assert.True(BaseFunc.IsNullOrEmpty((byte[])null!));
            Assert.True(BaseFunc.IsNullOrEmpty(Array.Empty<byte>()));
            Assert.False(BaseFunc.IsNullOrEmpty(new byte[] { 1, 2, 3 }));
        }

        [Fact]
        [DisplayName("IsEmpty(object) 對各種型別應走對應分支")]
        public void IsEmpty_Object_DispatchesToCorrectOverload()
        {
            Assert.True(BaseFunc.IsEmpty((object)null!));
            Assert.True(BaseFunc.IsEmpty((object)DBNull.Value));
            Assert.True(BaseFunc.IsEmpty((object)string.Empty));
            Assert.True(BaseFunc.IsEmpty((object)"   "));
            Assert.True(BaseFunc.IsEmpty((object)Guid.Empty));
            Assert.True(BaseFunc.IsEmpty((object)new List<int>()));
            Assert.True(BaseFunc.IsEmpty((object)DateTime.MinValue));

            Assert.False(BaseFunc.IsEmpty((object)"abc"));
            Assert.False(BaseFunc.IsEmpty((object)Guid.NewGuid()));
            Assert.False(BaseFunc.IsEmpty((object)new List<int> { 1 }));
            Assert.False(BaseFunc.IsEmpty((object)new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.False(BaseFunc.IsEmpty((object)123));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("   ", true)]
        [InlineData("abc", false)]
        [DisplayName("IsEmpty(string) 對 null/空字串/空白回傳 true")]
        public void IsEmpty_String_ReturnsExpectedResult(string? value, bool expected)
        {
            Assert.Equal(expected, BaseFunc.IsEmpty(value!));
        }

        [Fact]
        [DisplayName("IsEmpty(Guid) 對 Guid.Empty 回傳 true，對其他值回傳 false")]
        public void IsEmpty_Guid_ReturnsExpectedResult()
        {
            Assert.True(BaseFunc.IsEmpty(Guid.Empty));
            Assert.False(BaseFunc.IsEmpty(Guid.NewGuid()));
        }

        [Fact]
        [DisplayName("IsEmpty(DateTime) 對 MinValue/1753 之前回傳 true，對正常日期回傳 false")]
        public void IsEmpty_DateTime_ReturnsExpectedResult()
        {
            Assert.True(BaseFunc.IsEmpty(DateTime.MinValue));
            Assert.True(BaseFunc.IsEmpty(new DateTime(1752, 12, 31, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.False(BaseFunc.IsEmpty(new DateTime(1753, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)));
            Assert.False(BaseFunc.IsEmpty(new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Unspecified)));
        }

        [Fact]
        [DisplayName("IsEmpty(IList) 對 null 與空集合回傳 true，對有元素集合回傳 false")]
        public void IsEmpty_IList_ReturnsExpectedResult()
        {
            Assert.True(BaseFunc.IsEmpty((IList)null!));
            Assert.True(BaseFunc.IsEmpty((IList)new List<int>()));
            Assert.False(BaseFunc.IsEmpty((IList)new List<int> { 1, 2 }));
        }

        [Fact]
        [DisplayName("IsEmpty(IEnumerable) 對 null 與無元素回傳 true，對有元素回傳 false")]
        public void IsEmpty_IEnumerable_ReturnsExpectedResult()
        {
            Assert.True(BaseFunc.IsEmpty((IEnumerable)null!));
            Assert.True(BaseFunc.IsEmpty((IEnumerable)Array.Empty<int>()));
            Assert.False(BaseFunc.IsEmpty((IEnumerable)s_singleIntArray));
        }

        [Fact]
        [DisplayName("IsEmpty(byte[]) 對 null 與空陣列回傳 true，對有內容陣列回傳 false")]
        public void IsEmpty_ByteArray_ReturnsExpectedResult()
        {
            Assert.True(BaseFunc.IsEmpty((byte[])null!));
            Assert.True(BaseFunc.IsEmpty(Array.Empty<byte>()));
            Assert.False(BaseFunc.IsEmpty(new byte[] { 0x01 }));
        }

        #endregion

        #region Enum 名稱

        [Fact]
        [DisplayName("GetEnumName 對已定義 enum 回傳名稱，對未定義值回傳 null")]
        public void GetEnumName_DefinedAndUndefined_ReturnsExpectedResult()
        {
            Assert.Equal("Day", BaseFunc.GetEnumName(DateInterval.Day));
            Assert.Equal("Hour", BaseFunc.GetEnumName(DateInterval.Hour));
            // 未定義的 enum 值
            var undefined = (DateInterval)999;
            Assert.Null(BaseFunc.GetEnumName(undefined));
        }

        #endregion

        #region 型別轉換：CStr

        [Fact]
        [DisplayName("CStr(object) 對各種輸入回傳對應字串表示")]
        public void CStr_Object_ReturnsExpectedString()
        {
            Assert.Equal(string.Empty, BaseFunc.CStr(null!));
            Assert.Equal(string.Empty, BaseFunc.CStr(DBNull.Value));
            Assert.Equal("abc", BaseFunc.CStr("abc"));
            Assert.Equal("Day", BaseFunc.CStr(DateInterval.Day));
            Assert.Equal("123", BaseFunc.CStr(123));
        }

        [Fact]
        [DisplayName("CStr(object, defaultValue) 對 null/DBNull 回傳 defaultValue，對非 null 回傳字串")]
        public void CStr_ObjectWithDefault_ReturnsExpectedString()
        {
            Assert.Equal("N/A", BaseFunc.CStr(null!, "N/A"));
            Assert.Equal("N/A", BaseFunc.CStr(DBNull.Value, "N/A"));
            Assert.Equal("abc", BaseFunc.CStr("abc", "N/A"));
            Assert.Equal("Day", BaseFunc.CStr(DateInterval.Day, "N/A"));
        }

        #endregion

        #region 型別轉換：CBool

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
            Assert.Equal(expected, BaseFunc.CBool(value));
        }

        [Fact]
        [DisplayName("CBool(string) 對空字串回傳 defaultValue")]
        public void CBool_String_Empty_ReturnsDefault()
        {
            Assert.False(BaseFunc.CBool(""));
            Assert.True(BaseFunc.CBool("", true));
            Assert.True(BaseFunc.CBool(null!, true));
        }

        [Fact]
        [DisplayName("CBool(object) 對 bool 型別直接轉型，對其他型別透過字串轉換")]
        public void CBool_Object_ReturnsExpectedResult()
        {
            Assert.True(BaseFunc.CBool((object)true));
            Assert.False(BaseFunc.CBool((object)false));
            Assert.True(BaseFunc.CBool((object)"1"));
            Assert.False(BaseFunc.CBool((object)"0"));
            Assert.False(BaseFunc.CBool((object)null!));
            Assert.True(BaseFunc.CBool((object)null!, true));
        }

        #endregion

        #region 型別轉換：CEnum

        [Theory]
        [InlineData("Day", DateInterval.Day)]
        [InlineData("day", DateInterval.Day)] // Enum.Parse 已指定 ignoreCase=true
        [InlineData("Hour", DateInterval.Hour)]
        [DisplayName("CEnum(string, Type) 對合法字串回傳對應 enum 值（不區分大小寫）")]
        public void CEnum_ValidString_ReturnsEnumValue(string input, DateInterval expected)
        {
            // 本測試刻意呼叫 non-generic overload，驗證其行為
#pragma warning disable CA2263 // Prefer generic overload when type is known
            var result = BaseFunc.CEnum(input, typeof(DateInterval));
#pragma warning restore CA2263
            Assert.Equal(expected, (DateInterval)result);
        }

        [Fact]
        [DisplayName("CEnum<T>(string) 對合法字串回傳 enum 值，對非法字串拋出 ArgumentException")]
        public void CEnum_Generic_ValidAndInvalid_BehavesAsExpected()
        {
            Assert.Equal(DateInterval.Day, BaseFunc.CEnum<DateInterval>("Day"));
            Assert.Throws<ArgumentException>(() => BaseFunc.CEnum<DateInterval>("NotExist"));
        }

        #endregion

        #region 型別轉換：ConvertToNumber

        [Fact]
        [DisplayName("ConvertToNumber 對各種輸入回傳對應數值")]
        public void ConvertToNumber_VariousInputs_ReturnsExpectedResult()
        {
            Assert.Equal(0, BaseFunc.ConvertToNumber(null!));
            Assert.Equal(0, BaseFunc.ConvertToNumber(DBNull.Value));
            Assert.Equal(0, BaseFunc.ConvertToNumber(""));

            Assert.Equal((double)123.45, BaseFunc.ConvertToNumber("123.45"));
            Assert.Equal(1, BaseFunc.ConvertToNumber(true));
            Assert.Equal(0, BaseFunc.ConvertToNumber(false));
            Assert.Equal((int)DateInterval.Day, BaseFunc.ConvertToNumber(DateInterval.Day));
            Assert.Equal(123, BaseFunc.ConvertToNumber(123));
            Assert.Equal(123.45m, BaseFunc.ConvertToNumber(123.45m));
        }

        [Fact]
        [DisplayName("ConvertToNumber 對無法轉換的值拋出 InvalidCastException")]
        public void ConvertToNumber_InvalidInput_Throws()
        {
            Assert.Throws<InvalidCastException>(() => BaseFunc.ConvertToNumber(new object()));
        }

        #endregion

        #region 型別轉換：CInt / CDouble / CDecimal

        [Theory]
        [InlineData(null, 0)]
        [InlineData("", 0)]
        [InlineData("123", 123)]
        [InlineData("  ", 0)]
        [InlineData(123, 123)]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        [InlineData("abc", 0)] // 無法轉換回傳 defaultValue
        [DisplayName("CInt 對各種輸入回傳對應整數，無法轉換時回傳 defaultValue")]
        public void CInt_VariousInputs_ReturnsExpectedResult(object? value, int expected)
        {
            Assert.Equal(expected, BaseFunc.CInt(value!));
        }

        [Fact]
        [DisplayName("CInt 對 Enum 回傳對應整數")]
        public void CInt_Enum_ReturnsIntegerValue()
        {
            Assert.Equal((int)DateInterval.Day, BaseFunc.CInt(DateInterval.Day));
        }

        [Fact]
        [DisplayName("CDouble 對各種輸入回傳對應 double，無法轉換時回傳 defaultValue")]
        public void CDouble_VariousInputs_ReturnsExpectedResult()
        {
            Assert.Equal(0d, BaseFunc.CDouble(null!));
            Assert.Equal(0d, BaseFunc.CDouble(DBNull.Value));
            Assert.Equal(123.45, BaseFunc.CDouble("123.45"));
            Assert.Equal(1d, BaseFunc.CDouble(true));
            Assert.Equal(-1.5d, BaseFunc.CDouble("abc", -1.5));
        }

        [Fact]
        [DisplayName("CDecimal 對各種輸入回傳對應 decimal，無法轉換時回傳 defaultValue")]
        public void CDecimal_VariousInputs_ReturnsExpectedResult()
        {
            Assert.Equal(0m, BaseFunc.CDecimal(null!));
            Assert.Equal(0m, BaseFunc.CDecimal(DBNull.Value));
            Assert.Equal(123.45m, BaseFunc.CDecimal("123.45"));
            Assert.Equal(1m, BaseFunc.CDecimal(true));
            Assert.Equal(-1.5m, BaseFunc.CDecimal("abc", -1.5m));
        }

        #endregion

        #region 型別轉換：CDateTime / CDate

        [Fact]
        [DisplayName("CDateTime 對各種輸入回傳對應 DateTime")]
        public void CDateTime_VariousInputs_ReturnsExpectedResult()
        {
            Assert.Equal(default, BaseFunc.CDateTime(null!));
            Assert.Equal(default, BaseFunc.CDateTime(DBNull.Value));
            Assert.Equal(default, BaseFunc.CDateTime(""));

            var expected = new DateTime(2015, 3, 12, 0, 0, 0, DateTimeKind.Unspecified);
            Assert.Equal(expected, BaseFunc.CDateTime(expected));
            Assert.Equal(expected, BaseFunc.CDateTime("2015-03-12"));
            Assert.Equal(expected, BaseFunc.CDateTime("20150312"));

            // ROC date（民國年）
            Assert.Equal(expected, BaseFunc.CDateTime("1040312"));

            // 非數值字串 → StrToDate 回傳 DateTime.MinValue（不丟例外）
            Assert.Equal(DateTime.MinValue, BaseFunc.CDateTime("not-a-date"));
        }

        [Fact]
        [DisplayName("CDate 只保留日期部分")]
        public void CDate_ReturnsDatePortionOnly()
        {
            var input = new DateTime(2026, 4, 18, 15, 30, 45, DateTimeKind.Unspecified);
            var result = BaseFunc.CDate(input);
            Assert.Equal(new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Unspecified), result);
        }

        #endregion

        #region 型別轉換：CGuid

        [Fact]
        [DisplayName("CGuid(string) 對合法 Guid 字串回傳對應值，對空字串回傳 Guid.Empty，對非法字串拋出例外")]
        public void CGuid_String_BehavesAsExpected()
        {
            var guid = Guid.NewGuid();
            Assert.Equal(guid, BaseFunc.CGuid(guid.ToString()));
            Assert.Equal(Guid.Empty, BaseFunc.CGuid(string.Empty));
            Assert.Equal(Guid.Empty, BaseFunc.CGuid(null!));
            Assert.Throws<FormatException>(() => BaseFunc.CGuid("not-a-guid"));
        }

        [Fact]
        [DisplayName("CGuid(object) 對 null/DBNull/非字串回傳 Guid.Empty，對 Guid/合法字串回傳對應值")]
        public void CGuid_Object_BehavesAsExpected()
        {
            var guid = Guid.NewGuid();
            Assert.Equal(guid, BaseFunc.CGuid((object)guid));
            Assert.Equal(guid, BaseFunc.CGuid((object)guid.ToString()));
            Assert.Equal(Guid.Empty, BaseFunc.CGuid((object)null!));
            Assert.Equal(Guid.Empty, BaseFunc.CGuid((object)DBNull.Value));
            Assert.Equal(Guid.Empty, BaseFunc.CGuid((object)123));
        }

        #endregion

        #region DB 欄位轉換：CFieldValue / CDbFieldValue

        [Fact]
        [DisplayName("CFieldValue 依據 FieldDbType 走對應轉換分支")]
        public void CFieldValue_VariousDbTypes_ReturnsExpectedResult()
        {
            Assert.Equal("abc", BaseFunc.CFieldValue(FieldDbType.String, "abc"));
            Assert.Equal("abc", BaseFunc.CFieldValue(FieldDbType.Text, "abc"));
            Assert.True((bool)BaseFunc.CFieldValue(FieldDbType.Boolean, "1")!);
            Assert.Equal(123, BaseFunc.CFieldValue(FieldDbType.Integer, "123"));
            Assert.Equal(123.45m, BaseFunc.CFieldValue(FieldDbType.Decimal, "123.45"));
            Assert.Equal(123.45m, BaseFunc.CFieldValue(FieldDbType.Currency, "123.45"));

            var date = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Unspecified);
            Assert.Equal(date, BaseFunc.CFieldValue(FieldDbType.Date, "2026-04-18"));
            Assert.Equal(date, BaseFunc.CFieldValue(FieldDbType.DateTime, "2026-04-18"));

            var guid = Guid.NewGuid();
            Assert.Equal(guid, BaseFunc.CFieldValue(FieldDbType.Guid, guid.ToString()));

            // 未涵蓋的 FieldDbType 應原樣回傳
            var raw = new byte[] { 0x01, 0x02 };
            Assert.Same(raw, BaseFunc.CFieldValue(FieldDbType.Binary, raw));
        }

        [Fact]
        [DisplayName("CDbFieldValue 對 DateTime.MinValue 回傳 DBNull.Value，其餘走 CFieldValue")]
        public void CDbFieldValue_DateTimeMinValue_ReturnsDBNull()
        {
            Assert.Equal(DBNull.Value, BaseFunc.CDbFieldValue(FieldDbType.DateTime, DateTime.MinValue));
            Assert.Equal(DBNull.Value, BaseFunc.CDbFieldValue(FieldDbType.Date, DateTime.MinValue));

            // 一般日期走 CFieldValue
            var date = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Unspecified);
            Assert.Equal(date, BaseFunc.CDbFieldValue(FieldDbType.DateTime, date));
            Assert.Equal("abc", BaseFunc.CDbFieldValue(FieldDbType.String, "abc"));
        }

        #endregion

        #region GUID 生成

        [Fact]
        [DisplayName("NewGuid 每次呼叫應回傳不同的非空值")]
        public void NewGuid_ReturnsUniqueNonEmptyValue()
        {
            var g1 = BaseFunc.NewGuid();
            var g2 = BaseFunc.NewGuid();
            Assert.NotEqual(Guid.Empty, g1);
            Assert.NotEqual(Guid.Empty, g2);
            Assert.NotEqual(g1, g2);
        }

        [Fact]
        [DisplayName("NewGuidString 回傳合法的 Guid 字串格式")]
        public void NewGuidString_ReturnsValidGuidString()
        {
            var s = BaseFunc.NewGuidString();
            Assert.False(string.IsNullOrEmpty(s));
            Assert.True(Guid.TryParse(s, out var parsed));
            Assert.NotEqual(Guid.Empty, parsed);
        }

        #endregion

        #region 命令列引數解析：ParseCommandLineArgs

        [Fact]
        [DisplayName("ParseCommandLineArgs 空陣列回傳空字典")]
        public void ParseCommandLineArgs_EmptyArray_ReturnsEmptyDictionary()
        {
            var result = BaseFunc.ParseCommandLineArgs(Array.Empty<string>());
            Assert.Empty(result);
        }

        [Fact]
        [DisplayName("ParseCommandLineArgs 僅有執行檔名稱時回傳空字典")]
        public void ParseCommandLineArgs_OnlyExecutable_ReturnsEmptyDictionary()
        {
            var result = BaseFunc.ParseCommandLineArgs(s_argsOnlyExe);
            Assert.Empty(result);
        }

        [Fact]
        [DisplayName("ParseCommandLineArgs 跳過不以 -- 開頭的引數")]
        public void ParseCommandLineArgs_IgnoresNonOptionArgs()
        {
            var result = BaseFunc.ParseCommandLineArgs(s_argsWithPositional);
            Assert.Empty(result);
        }

        [Fact]
        [DisplayName("ParseCommandLineArgs --key value 形式解析為鍵值對")]
        public void ParseCommandLineArgs_KeyValuePair_ParsesCorrectly()
        {
            var result = BaseFunc.ParseCommandLineArgs(s_argsKeyValue);
            Assert.Equal(2, result.Count);
            Assert.Equal("bee", result["name"]);
            Assert.Equal("42", result["count"]);
        }

        [Fact]
        [DisplayName("ParseCommandLineArgs --flag 後接新選項時設為 true")]
        public void ParseCommandLineArgs_FlagFollowedByOption_DefaultsToTrue()
        {
            var result = BaseFunc.ParseCommandLineArgs(s_argsFlagThenOption);
            Assert.Equal("true", result["verbose"]);
            Assert.Equal("bee", result["name"]);
        }

        [Fact]
        [DisplayName("ParseCommandLineArgs --flag 結尾時設為 true")]
        public void ParseCommandLineArgs_FlagAtEnd_DefaultsToTrue()
        {
            var result = BaseFunc.ParseCommandLineArgs(s_argsFlagAtEnd);
            Assert.Equal("true", result["flag"]);
        }

        [Fact]
        [DisplayName("ParseCommandLineArgs 鍵名以大小寫無關方式檢索")]
        public void ParseCommandLineArgs_KeyComparison_IsCaseInsensitive()
        {
            var result = BaseFunc.ParseCommandLineArgs(s_argsCaseInsensitive);
            Assert.Equal("bee", result["name"]);
            Assert.Equal("bee", result["NAME"]);
        }

        [Fact]
        [DisplayName("GetCommandLineArgs 可呼叫並回傳字典")]
        public void GetCommandLineArgs_ReturnsDictionary()
        {
            var result = BaseFunc.GetCommandLineArgs();
            Assert.NotNull(result);
        }

        #endregion
    }
}
