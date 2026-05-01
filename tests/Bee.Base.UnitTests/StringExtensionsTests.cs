using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// Tests for <see cref="StringExtensions"/> covering operations BCL does not provide:
    /// out-parameter split and conditional prefix / suffix removal. All comparisons default
    /// to case-insensitive (the framework convention).
    /// </summary>
    public class StringExtensionsTests
    {
        // ---- SplitLeft / SplitRight ----

        [Fact]
        [DisplayName("SplitLeft 應於找到 delimiter 時回傳左右兩段")]
        public void SplitLeft_SplitsIntoLeftAndRight()
        {
            "alpha-beta-gamma".SplitLeft("-", out var left, out var right);
            Assert.Equal("alpha", left);
            Assert.Equal("beta-gamma", right);
        }

        [Fact]
        [DisplayName("SplitLeft 於找不到 delimiter 時應回傳兩個空字串")]
        public void SplitLeft_NotFound_ReturnsEmpty()
        {
            "alpha".SplitLeft("-", out var left, out var right);
            Assert.Equal(string.Empty, left);
            Assert.Equal(string.Empty, right);
        }

        [Fact]
        [DisplayName("SplitRight 應於從右起找 delimiter 時回傳左右兩段")]
        public void SplitRight_SplitsAtLastDelimiter()
        {
            "alpha-beta-gamma".SplitRight("-", out var left, out var right);
            Assert.Equal("alpha-beta", left);
            Assert.Equal("gamma", right);
        }

        // ---- LeftCut / RightCut / LeftRightCut(case-insensitive) ----

        [Fact]
        [DisplayName("LeftCut 於有前綴時應移除,大小寫不敏感;否則原樣回傳")]
        public void LeftCut_ByPrefix_CaseInsensitive()
        {
            Assert.Equal("cde", "abcde".LeftCut("AB"));
            Assert.Equal("abcde", "abcde".LeftCut("XY"));
            Assert.Equal(string.Empty, ((string?)null).LeftCut("x"));
        }

        [Fact]
        [DisplayName("RightCut 於有後綴時應移除,大小寫不敏感;否則原樣回傳")]
        public void RightCut_BySuffix_CaseInsensitive()
        {
            Assert.Equal("abc", "abcDE".RightCut("de"));
            Assert.Equal("abcde", "abcde".RightCut("XY"));
            Assert.Equal(string.Empty, ((string?)null).RightCut("x"));
        }

        [Fact]
        [DisplayName("LeftRightCut 應同時移除前綴與後綴")]
        public void LeftRightCut_RemovesBoth()
        {
            Assert.Equal("abc", "[abc]".LeftRightCut("[", "]"));
        }
    }
}
