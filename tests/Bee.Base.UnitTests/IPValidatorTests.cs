using System.ComponentModel;

namespace Bee.Base.UnitTests
{
    public class IPValidatorTests
    {
        [Fact]
        [DisplayName("IP 驗證器應正確判斷白名單允許與黑名單拒絕的 IP")]
        public void IsIpAllowed_WhitelistAndBlacklist_ReturnsExpectedResult()
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
    }
}
