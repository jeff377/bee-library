using System.ComponentModel;
using System.Data;
using System.Text;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// StrFunc 補強測試:涵蓋既有 <see cref="StrFuncTests"/> 未覆蓋的方法與分支。
    /// </summary>
    public class StrFuncExtraTests
    {
        // ---- IsEmpty / IsNotEmpty ----

        [Theory]
        [DisplayName("IsEmpty(string) 應依 null/空字串/空白/isTrim 參數回傳正確結果")]
        [InlineData(null, true, true)]
        [InlineData("", true, true)]
        [InlineData("  ", true, true)]
        [InlineData("  ", false, false)]
        [InlineData("abc", true, false)]
        public void IsEmpty_String_ReturnsExpected(string? input, bool isTrim, bool expected)
        {
            Assert.Equal(expected, StrFunc.IsEmpty(input, isTrim));
        }

        [Fact]
        [DisplayName("IsEmpty(object) 應處理 null/DBNull/字串")]
        public void IsEmpty_Object_HandlesNullAndDbNull()
        {
            Assert.True(StrFunc.IsEmpty((object)null!));
            Assert.True(StrFunc.IsEmpty(DBNull.Value));
            Assert.True(StrFunc.IsEmpty((object)"  "));
            Assert.False(StrFunc.IsEmpty((object)"x"));
        }

        [Fact]
        [DisplayName("IsNotEmpty 應為 IsEmpty 的反向")]
        public void IsNotEmpty_IsInverseOfIsEmpty()
        {
            Assert.False(StrFunc.IsNotEmpty((string?)null));
            Assert.False(StrFunc.IsNotEmpty(""));
            Assert.True(StrFunc.IsNotEmpty("x"));
            Assert.True(StrFunc.IsNotEmpty((object)"x"));
            Assert.False(StrFunc.IsNotEmpty((object)null!));
        }

        // ---- Format ----

        [Fact]
        [DisplayName("Format(string, args) 應以 string.Format 展開")]
        public void Format_WithArgs_Expands()
        {
            Assert.Equal("a=1,b=2", StrFunc.Format("a={0},b={1}", 1, 2));
        }

        [Fact]
        [DisplayName("Format(string, DataRow, columnNames) 應以欄位值展開")]
        public void Format_WithDataRow_UsesColumnValues()
        {
            var table = new DataTable();
            table.Columns.Add("A", typeof(string));
            table.Columns.Add("B", typeof(int));
            var row = table.NewRow();
            row["A"] = "hello";
            row["B"] = 42;

            Assert.Equal("hello:42", StrFunc.Format("{0}:{1}", row, "A", "B"));
        }

        [Fact]
        [DisplayName("Format(string, DataRow, null) 應回傳原字串")]
        public void Format_WithDataRowNullArgs_ReturnsOriginal()
        {
            var row = new DataTable().NewRow();
            Assert.Equal("no-change", StrFunc.Format("no-change", row, null!));
        }

        // ---- IsEquals / IsEqualsOr ----

        [Theory]
        [DisplayName("IsEquals 應處理 null 情境")]
        [InlineData(null, null, true)]
        [InlineData(null, "x", false)]
        [InlineData("x", null, false)]
        public void IsEquals_NullHandling(string? s1, string? s2, bool expected)
        {
            Assert.Equal(expected, StrFunc.IsEquals(s1!, s2!));
        }

        [Theory]
        [DisplayName("IsEquals 依 trim/ignoreCase 參數比對")]
        [InlineData("AB", "ab", false, true, true)]
        [InlineData("AB", "ab", false, false, false)]
        [InlineData(" ab ", "ab", true, true, true)]
        [InlineData(" ab ", "ab", false, true, false)]
        public void IsEquals_TrimAndCase(string a, string b, bool isTrim, bool ignoreCase, bool expected)
        {
            Assert.Equal(expected, StrFunc.IsEquals(a, b, isTrim, ignoreCase));
        }

        [Fact]
        [DisplayName("IsEqualsOr 應在任一項相等時回傳 true")]
        public void IsEqualsOr_MatchesAny()
        {
            Assert.True(StrFunc.IsEqualsOr("hello", "world", "Hello", "foo"));
            Assert.False(StrFunc.IsEqualsOr("hello", "world", "foo"));
        }

        // ---- ToUpper / ToLower ----

        [Fact]
        [DisplayName("ToUpper / ToLower 於 null 應回傳空字串")]
        public void ToUpperLower_NullReturnsEmpty()
        {
            Assert.Equal(string.Empty, StrFunc.ToUpper(null!));
            Assert.Equal(string.Empty, StrFunc.ToLower(null!));
            Assert.Equal("AB", StrFunc.ToUpper("ab"));
            Assert.Equal("ab", StrFunc.ToLower("AB"));
        }

        // ---- Replace ----

        [Fact]
        [DisplayName("Replace 應替換字串並支援大小寫忽略")]
        public void Replace_WorksCaseInsensitive()
        {
            Assert.Equal("XYZXYZ", StrFunc.Replace("abcABC", "abc", "XYZ", ignoreCase: true));
            Assert.Equal("XYZABC", StrFunc.Replace("abcABC", "abc", "XYZ", ignoreCase: false));
        }

        [Fact]
        [DisplayName("Replace 於空字串應回傳空字串")]
        public void Replace_EmptyInput_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, StrFunc.Replace(string.Empty, "x", "y"));
        }

        // ---- Split ----

        [Fact]
        [DisplayName("Split 應依分隔字串切分")]
        public void Split_Splits()
        {
            Assert.Equal(new[] { "a", "b", "c" }, StrFunc.Split("a||b||c", "||"));
        }

        [Fact]
        [DisplayName("Split 於空字串應回傳空陣列")]
        public void Split_Empty_ReturnsEmptyArray()
        {
            Assert.Empty(StrFunc.Split(string.Empty, ","));
        }

        [Fact]
        [DisplayName("SplitNewLine 應同時處理 \\r\\n 與 \\n")]
        public void SplitNewLine_HandlesBothEndings()
        {
            Assert.Equal(new[] { "a", "b", "c" }, StrFunc.SplitNewLine("a\r\nb\nc"));
            Assert.Empty(StrFunc.SplitNewLine(string.Empty));
        }

        [Fact]
        [DisplayName("SplitLeft 應於找到 delimiter 時回傳左右兩段")]
        public void SplitLeft_SplitsIntoLeftAndRight()
        {
            StrFunc.SplitLeft("alpha-beta-gamma", "-", out var left, out var right);
            Assert.Equal("alpha", left);
            Assert.Equal("beta-gamma", right);
        }

        [Fact]
        [DisplayName("SplitRight 應於從右起找 delimiter 時回傳左右兩段")]
        public void SplitRight_SplitsAtLastDelimiter()
        {
            StrFunc.SplitRight("alpha-beta-gamma", "-", out var left, out var right);
            Assert.Equal("alpha-beta", left);
            Assert.Equal("gamma", right);
        }

        // ---- Append / Merge ----

        [Fact]
        [DisplayName("Append 首次應不加 delimiter,之後每次加")]
        public void Append_OnlyDelimiterAfterFirst()
        {
            var sb = new StringBuilder();
            StrFunc.Append(sb, "a", ",");
            StrFunc.Append(sb, "b", ",");
            StrFunc.Append(sb, "c", ",");
            Assert.Equal("a,b,c", sb.ToString());
        }

        [Fact]
        [DisplayName("Merge(string) 於 s1 為空時應不加 delimiter")]
        public void Merge_EmptyFirst_NoDelimiter()
        {
            Assert.Equal("b", StrFunc.Merge(string.Empty, "b", ","));
            Assert.Equal("a,b", StrFunc.Merge("a", "b", ","));
        }

        [Fact]
        [DisplayName("Merge(StringBuilder) 應與 Append 行為一致")]
        public void Merge_StringBuilder_BehavesLikeAppend()
        {
            var sb = new StringBuilder();
            StrFunc.Merge(sb, "a", ";");
            StrFunc.Merge(sb, "b", ";");
            Assert.Equal("a;b", sb.ToString());
        }

        // ---- Left / Right ----

        [Theory]
        [DisplayName("Left / Right 於空字串或 length<=0 應回傳空字串")]
        [InlineData("", 5)]
        [InlineData("abc", 0)]
        [InlineData("abc", -1)]
        public void LeftRight_InvalidInput_ReturnsEmpty(string input, int length)
        {
            Assert.Equal(string.Empty, StrFunc.Left(input, length));
            Assert.Equal(string.Empty, StrFunc.Right(input, length));
        }

        [Fact]
        [DisplayName("Left / Right 正常切取")]
        public void LeftRight_ValidInput()
        {
            Assert.Equal("ab", StrFunc.Left("abcde", 2));
            Assert.Equal("de", StrFunc.Right("abcde", 2));
        }

        [Fact]
        [DisplayName("LeftWith / RightWith 應忽略大小寫")]
        public void LeftWithRightWith_CaseInsensitive()
        {
            Assert.True(StrFunc.LeftWith("Hello", "he"));
            Assert.True(StrFunc.RightWith("Hello", "LO"));
            Assert.False(StrFunc.LeftWith(string.Empty, "he"));
            Assert.False(StrFunc.RightWith(string.Empty, "lo"));
        }

        // ---- LeftCut / RightCut / LeftRightCut ----

        [Fact]
        [DisplayName("LeftCut(int) 應從左邊移除指定長度")]
        public void LeftCut_ByLength()
        {
            Assert.Equal("cde", StrFunc.LeftCut("abcde", 2));
        }

        [Fact]
        [DisplayName("LeftCut(string) 於有前綴時應移除,否則原樣回傳")]
        public void LeftCut_ByPrefix()
        {
            Assert.Equal("cde", StrFunc.LeftCut("abcde", "AB"));  // 忽略大小寫
            Assert.Equal("abcde", StrFunc.LeftCut("abcde", "XY"));
        }

        [Fact]
        [DisplayName("RightCut(int) 應從右邊移除指定長度")]
        public void RightCut_ByLength()
        {
            Assert.Equal("abc", StrFunc.RightCut("abcde", 2));
        }

        [Fact]
        [DisplayName("RightCut(string) 於有後綴時應移除,否則原樣回傳")]
        public void RightCut_BySuffix()
        {
            Assert.Equal("abc", StrFunc.RightCut("abcDE", "de"));  // 忽略大小寫
            Assert.Equal("abcde", StrFunc.RightCut("abcde", "XY"));
        }

        [Fact]
        [DisplayName("LeftRightCut 應同時移除前綴與後綴")]
        public void LeftRightCut_RemovesBoth()
        {
            Assert.Equal("abc", StrFunc.LeftRightCut("[abc]", "[", "]"));
        }

        // ---- Substring ----

        [Theory]
        [DisplayName("Substring(start) 應處理負值/空字串")]
        [InlineData("", 0, "")]
        [InlineData("abc", -1, "abc")]
        [InlineData("abcde", 2, "cde")]
        public void Substring_OneArg_HandlesEdges(string input, int start, string expected)
        {
            Assert.Equal(expected, StrFunc.Substring(input, start));
        }

        [Theory]
        [DisplayName("Substring(start,length) 應處理空字串/長度超界/負 start")]
        [InlineData("", 0, 3, "")]
        [InlineData("abcde", 0, 0, "")]
        [InlineData("abcde", -1, 2, "ab")]
        [InlineData("abcde", 1, 2, "bc")]
        [InlineData("abcde", 3, 100, "de")]
        public void Substring_TwoArgs_HandlesEdges(string input, int start, int length, string expected)
        {
            Assert.Equal(expected, StrFunc.Substring(input, start, length));
        }

        // ---- Pos / PosRev / Contains ----

        [Fact]
        [DisplayName("Pos / PosRev 應回傳正確索引;找不到或空字串回 -1")]
        public void PosAndPosRev()
        {
            Assert.Equal(0, StrFunc.Pos("hello", "HE"));
            Assert.Equal(3, StrFunc.PosRev("abab", "b"));
            Assert.Equal(-1, StrFunc.Pos(string.Empty, "x"));
            Assert.Equal(-1, StrFunc.PosRev(string.Empty, "x"));
            Assert.Equal(-1, StrFunc.Pos("abc", "xyz"));
        }

        [Fact]
        [DisplayName("Contains 應對應 Pos 結果")]
        public void Contains_Matches()
        {
            Assert.True(StrFunc.Contains("abcdef", "CD"));
            Assert.False(StrFunc.Contains("abc", "xyz"));
        }

        // ---- Trim ----

        [Fact]
        [DisplayName("Trim 於 null 應回傳空字串,並移除 ZWSP/ZWNBSP")]
        public void Trim_NullAndZeroWidth()
        {
            Assert.Equal(string.Empty, StrFunc.Trim(null!));
            Assert.Equal("abc", StrFunc.Trim("  abc  "));
            Assert.Equal("abc", StrFunc.Trim("\u200Babc\uFEFF"));
        }

        // ---- Length / PadLeft / Dup ----

        [Fact]
        [DisplayName("Length 於空字串回 0")]
        public void Length_EmptyReturnsZero()
        {
            Assert.Equal(0, StrFunc.Length(string.Empty));
            Assert.Equal(3, StrFunc.Length("abc"));
        }

        [Fact]
        [DisplayName("PadLeft 應以指定字元補足左側")]
        public void PadLeft_PadsLeft()
        {
            Assert.Equal("00005", StrFunc.PadLeft("5", 5, '0'));
        }

        [Fact]
        [DisplayName("Dup 應回傳重複字元的字串")]
        public void Dup_RepeatsCharacter()
        {
            Assert.Equal("---", StrFunc.Dup(3, '-'));
            Assert.Equal(string.Empty, StrFunc.Dup(0, '-'));
        }

        // ---- GetNextId overloads ----

        [Fact]
        [DisplayName("GetNextId(string baseValues) 應依字元序產生下一個 id")]
        public void GetNextId_CustomBaseValues()
        {
            // baseValues = "ABC",進位邏輯以 A 為 0、B 為 1、C 為 2
            Assert.Equal("AB", StrFunc.GetNextId("AA", "ABC"));
            Assert.Equal("BA", StrFunc.GetNextId("AC", "ABC"));
        }

        [Fact]
        [DisplayName("Like 於 source 或 pattern 為 null 時應回傳 false")]
        public void Like_NullInputs_ReturnsFalse()
        {
            Assert.False(StrFunc.Like(null!, "*"));
            Assert.False(StrFunc.Like("abc", null!));
        }
    }
}
