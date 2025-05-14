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
            var whitelist = new List<string>
            {
                "192.168.1.*",
                "10.0.*.*",
                "192.168.2.0/24"
            };

            // 定義黑名單
            var blacklist = new List<string>
            {
                "192.168.1.100",
                "10.0.0.5",
                "192.168.3.0/24"
            };

            // 初始化驗證器
            var validator = new TIPValidator(whitelist, blacklist);

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
            Assert.True(BaseFunc.IsNumeric(EDateInterval.Day));
            Assert.True(BaseFunc.IsNumeric(EDateInterval.Hour));

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


    }
}