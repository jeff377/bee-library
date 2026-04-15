using System.ComponentModel;
using System.Globalization;

namespace Bee.Base.UnitTests
{
    public class StrFuncTests
    {
        [Theory]
        [InlineData("hello", "he*", true, CompareOptions.IgnoreCase)]
        [InlineData("hello", "he?lo", true, CompareOptions.IgnoreCase)]
        [InlineData("hello", "he#lo", false, CompareOptions.IgnoreCase)]
        [InlineData("h3llo", "h#llo", true, CompareOptions.IgnoreCase)]
        [InlineData("Hello", "h*", true, CompareOptions.IgnoreCase)]
        [InlineData("Hello", "h*", false, CompareOptions.None)] // ✅ 明確區分大小寫
        [DisplayName("Like 萬用字元比對應回傳正確結果")]
        public void Like_PatternWithOptions_ReturnsExpectedMatch(string input, string pattern, bool expected, CompareOptions options)
        {
            var result = StrFunc.Like(input, pattern, options);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("0009", 10, "0010")]
        [InlineData("0999", 10, "1000")]
        [InlineData("Z9", 36, "ZA")]
        [InlineData("ZZ", 36, "100")]
        [InlineData("ABZ", 36, "AC0")] // 正確結果
        [InlineData("ZZZ", 36, "1000")]
        [DisplayName("GetNextId 應回傳正確的下一個編號")]
        public void GetNextId_VariousBaseAndId_ReturnsExpectedNextId(string currentId, int numberBase, string expected)
        {
            var next = StrFunc.GetNextId(currentId, numberBase);
            Assert.Equal(expected, next);
        }
    }
}
