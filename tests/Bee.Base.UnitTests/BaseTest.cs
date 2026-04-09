using System.Globalization;
using System.Text;

namespace Bee.Base.UnitTests
{
    public class BaseTest
    {
        /// <summary>
        /// IP 位址驗證。
        /// </summary>
        [Fact]
        public void IPValidator()
        {
            // 定義白名單
            var whitelist = new System.Collections.Generic.List<string>
            {
                "192.168.1.*",
                "10.0.*.*",
                "192.168.2.0/24"
            };

            // 定義黑名單
            var blacklist = new System.Collections.Generic.List<string>
            {
                "192.168.1.100",
                "10.0.0.5",
                "192.168.3.0/24"
            };

            // 初始化驗證器
            var validator = new IPValidator(whitelist, blacklist);

            // 檢查 IP 地址是否被允許
            var allowed = validator.IsIpAllowed("192.168.2.50");
            Assert.True(allowed);  // 比較回傳值與預期值
            var allowed2 = validator.IsIpAllowed("10.0.0.5");
            Assert.False(allowed2);  // 比較回傳值與預期值
        }

        /// <summary>
        /// 測試 IsNumeric 方法。
        /// </summary>
        [Fact]
        public void IsNumericTest()
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
            Assert.False(BaseFunc.IsNumeric(null));
            Assert.False(BaseFunc.IsNumeric(new object()));
            Assert.False(BaseFunc.IsNumeric(DateTime.Now));
        }

        [Theory]
        [InlineData("hello", "he*", true, CompareOptions.IgnoreCase)]
        [InlineData("hello", "he?lo", true, CompareOptions.IgnoreCase)]
        [InlineData("hello", "he#lo", false, CompareOptions.IgnoreCase)]
        [InlineData("h3llo", "h#llo", true, CompareOptions.IgnoreCase)]
        [InlineData("Hello", "h*", true, CompareOptions.IgnoreCase)]
        [InlineData("Hello", "h*", false, CompareOptions.None)] // ✅ 明確區分大小寫
        public void LikePatternTest(string input, string pattern, bool expected, CompareOptions options)
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
        public void GetNextIdTest(string currentId, int numberBase, string expected)
        {
            var next = StrFunc.GetNextId(currentId, numberBase);
            Assert.Equal(expected, next);
        }

        [Fact]
        public void MemberPathTest()
        {
            // Act
            var path = MemberPath.Of(() => SysInfo.Version);

            // Assert
            Assert.Equal("SysInfo.Version", path);
        }

        [Fact]
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
    }
}