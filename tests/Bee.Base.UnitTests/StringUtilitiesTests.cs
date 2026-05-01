using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// Tests for <see cref="StringUtilities"/> covering the framework's encapsulated
    /// defaults (IgnoreCase comparison / InvariantCulture formatting / null-safe handling).
    /// </summary>
    public class StringUtilitiesTests
    {
        private static readonly string[] s_expectedAbc = ["a", "b", "c"];

        // ---- IsEmpty / IsNotEmpty ----

        [Theory]
        [InlineData(null, true, true)]
        [InlineData("", true, true)]
        [InlineData("  ", true, true)]
        [InlineData("  ", false, false)]
        [InlineData("abc", true, false)]
        [DisplayName("IsEmpty(string) 應依 null/空字串/空白/isTrim 參數回傳正確結果")]
        public void IsEmpty_String_ReturnsExpected(string? input, bool isTrim, bool expected)
        {
            Assert.Equal(expected, StringUtilities.IsEmpty(input, isTrim));
        }

        [Fact]
        [DisplayName("IsEmpty(object) 應處理 null/DBNull/字串")]
        public void IsEmpty_Object_HandlesNullAndDbNull()
        {
            Assert.True(StringUtilities.IsEmpty((object?)null));
            Assert.True(StringUtilities.IsEmpty(DBNull.Value));
            Assert.True(StringUtilities.IsEmpty((object)"  "));
            Assert.False(StringUtilities.IsEmpty((object)"x"));
        }

        [Fact]
        [DisplayName("IsNotEmpty 應為 IsEmpty 的反向")]
        public void IsNotEmpty_IsInverseOfIsEmpty()
        {
            Assert.False(StringUtilities.IsNotEmpty((string?)null));
            Assert.False(StringUtilities.IsNotEmpty(""));
            Assert.True(StringUtilities.IsNotEmpty("x"));
            Assert.True(StringUtilities.IsNotEmpty((object)"x"));
            Assert.False(StringUtilities.IsNotEmpty((object?)null));
        }

        // ---- Format ----

        [Fact]
        [DisplayName("Format 應以 InvariantCulture 展開,無需 caller 傳 culture")]
        public void Format_WithArgs_Expands()
        {
            Assert.Equal("a=1,b=2", StringUtilities.Format("a={0},b={1}", 1, 2));
        }

        // ---- IsEquals / IsEqualsOr (default IgnoreCase) ----

        [Theory]
        [InlineData(null, null, true)]
        [InlineData(null, "x", false)]
        [InlineData("x", null, false)]
        [DisplayName("IsEquals 應處理 null 情境")]
        public void IsEquals_NullHandling(string? s1, string? s2, bool expected)
        {
            Assert.Equal(expected, StringUtilities.IsEquals(s1, s2));
        }

        [Theory]
        [InlineData("AB", "ab", true, true)]
        [InlineData("AB", "ab", false, false)]
        [InlineData("ab", "ab", false, true)]
        [DisplayName("IsEquals 預設 IgnoreCase,可用 ignoreCase=false 切換")]
        public void IsEquals_IgnoreCase(string a, string b, bool ignoreCase, bool expected)
        {
            Assert.Equal(expected, StringUtilities.IsEquals(a, b, ignoreCase));
        }

        [Fact]
        [DisplayName("IsEqualsOr 應在任一項相等時回傳 true(預設 IgnoreCase)")]
        public void IsEqualsOr_MatchesAny()
        {
            Assert.True(StringUtilities.IsEqualsOr("hello", "world", "Hello", "foo"));
            Assert.False(StringUtilities.IsEqualsOr("hello", "world", "foo"));
        }

        // ---- Contains / StartsWith / EndsWith / IndexOf / LastIndexOf (default IgnoreCase) ----

        [Fact]
        [DisplayName("Contains 預設 IgnoreCase 比對")]
        public void Contains_DefaultIgnoreCase()
        {
            Assert.True(StringUtilities.Contains("abcdef", "CD"));
            Assert.False(StringUtilities.Contains("abc", "xyz"));
            Assert.False(StringUtilities.Contains(null, "x"));
        }

        [Fact]
        [DisplayName("StartsWith / EndsWith 預設 IgnoreCase")]
        public void StartsWithEndsWith_DefaultIgnoreCase()
        {
            Assert.True(StringUtilities.StartsWith("Hello", "he"));
            Assert.True(StringUtilities.EndsWith("Hello", "LO"));
            Assert.False(StringUtilities.StartsWith(string.Empty, "he"));
            Assert.False(StringUtilities.EndsWith(string.Empty, "lo"));
            Assert.False(StringUtilities.StartsWith(null, "x"));
            Assert.False(StringUtilities.EndsWith(null, "x"));
        }

        [Fact]
        [DisplayName("IndexOf / LastIndexOf 預設 IgnoreCase;找不到或 null 回 -1")]
        public void IndexOfLastIndexOf_DefaultIgnoreCase()
        {
            Assert.Equal(0, StringUtilities.IndexOf("hello", "HE"));
            Assert.Equal(3, StringUtilities.LastIndexOf("abab", "b"));
            Assert.Equal(-1, StringUtilities.IndexOf(string.Empty, "x"));
            Assert.Equal(-1, StringUtilities.LastIndexOf(string.Empty, "x"));
            Assert.Equal(-1, StringUtilities.IndexOf("abc", "xyz"));
            Assert.Equal(-1, StringUtilities.IndexOf(null, "x"));
        }

        // ---- Replace ----

        [Fact]
        [DisplayName("Replace 預設 IgnoreCase,可用 ignoreCase=false 切換")]
        public void Replace_DefaultIgnoreCase()
        {
            Assert.Equal("XYZXYZ", StringUtilities.Replace("abcABC", "abc", "XYZ"));
            Assert.Equal("XYZABC", StringUtilities.Replace("abcABC", "abc", "XYZ", ignoreCase: false));
        }

        [Fact]
        [DisplayName("Replace 於空字串應回傳空字串")]
        public void Replace_EmptyInput_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, StringUtilities.Replace(string.Empty, "x", "y"));
        }

        // ---- Split ----

        [Fact]
        [DisplayName("Split 應依分隔字串切分")]
        public void Split_Splits()
        {
            Assert.Equal(s_expectedAbc, StringUtilities.Split("a||b||c", "||"));
        }

        [Fact]
        [DisplayName("Split 於空字串應回傳空陣列(框架語意)")]
        public void Split_Empty_ReturnsEmptyArray()
        {
            Assert.Empty(StringUtilities.Split(string.Empty, ","));
            Assert.Empty(StringUtilities.Split(null, ","));
        }

        // ---- Trim ----

        [Fact]
        [DisplayName("Trim 於 null 應回傳空字串,並移除 ZWSP/ZWNBSP")]
        public void Trim_NullAndZeroWidth()
        {
            Assert.Equal(string.Empty, StringUtilities.Trim(null));
            Assert.Equal("abc", StringUtilities.Trim("  abc  "));
            Assert.Equal("abc", StringUtilities.Trim("​abc﻿"));
        }

        // ---- GetNextId ----

        [Theory]
        [InlineData("0009", 10, "0010")]
        [InlineData("0999", 10, "1000")]
        [InlineData("Z9", 36, "ZA")]
        [InlineData("ZZ", 36, "100")]
        [InlineData("ABZ", 36, "AC0")]
        [InlineData("ZZZ", 36, "1000")]
        [DisplayName("GetNextId 應回傳正確的下一個編號")]
        public void GetNextId_VariousBaseAndId_ReturnsExpectedNextId(string currentId, int numberBase, string expected)
        {
            Assert.Equal(expected, StringUtilities.GetNextId(currentId, numberBase));
        }

        [Fact]
        [DisplayName("GetNextId(value, baseValues) 應依字元序產生下一個 id")]
        public void GetNextId_CustomBaseValues()
        {
            Assert.Equal("AB", StringUtilities.GetNextId("AA", "ABC"));
            Assert.Equal("BA", StringUtilities.GetNextId("AC", "ABC"));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(37)]
        [DisplayName("GetNextId(value, numberBase) 進位基數超出 2-36 應拋 ArgumentOutOfRangeException")]
        public void GetNextId_NumberBaseOutOfRange_Throws(int numberBase)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => StringUtilities.GetNextId("A", numberBase));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [DisplayName("GetNextId(value, baseValues) baseValues 為 null 或空字串應拋 ArgumentException")]
        public void GetNextId_EmptyBaseValues_Throws(string? baseValues)
        {
            Assert.Throws<ArgumentException>(() => StringUtilities.GetNextId("A", baseValues!));
        }

        [Fact]
        [DisplayName("GetNextId(value, baseValues) value 含 baseValues 外字元應拋 ArgumentException")]
        public void GetNextId_InvalidCharacterInValue_Throws()
        {
            Assert.Throws<ArgumentException>(() => StringUtilities.GetNextId("AZ", "AB"));
        }
    }
}
