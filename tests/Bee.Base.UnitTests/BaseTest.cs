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
        /// 加解密測試。
        /// </summary>
        [Fact]
        public void Encryption()
        {
            byte[] oSrcBytes;
            byte[] oDstBytes;
            string sSrcValue;
            string sDstValue;
            string sKey;
            string sIV;
            string sEncryption;

            sKey = StrFunc.Left(Guid.NewGuid().ToString().Replace("-", ""), 32);
            sIV = StrFunc.Left(Guid.NewGuid().ToString().Replace("-", ""), 16);

            sSrcValue = "壓縮測試文字";
            oSrcBytes = Encoding.UTF8.GetBytes(sSrcValue);
            oDstBytes = EncryptionFunc.AesEncrypt(oSrcBytes, sKey, sIV);
            oSrcBytes = EncryptionFunc.AesDecrypt(oDstBytes, sKey, sIV);
            sDstValue = Encoding.UTF8.GetString(oSrcBytes);
            Assert.Equal(sSrcValue, sDstValue);

            oSrcBytes = Encoding.UTF8.GetBytes(sSrcValue);
            oDstBytes = EncryptionFunc.AesEncrypt(oSrcBytes);
            oSrcBytes = EncryptionFunc.AesDecrypt(oDstBytes);
            sDstValue = Encoding.UTF8.GetString(oSrcBytes);
            Assert.Equal(sSrcValue, sDstValue);

            sEncryption = EncryptionFunc.AesEncrypt(sSrcValue);
            sDstValue = EncryptionFunc.AesDecrypt(sEncryption);
            Assert.Equal(sSrcValue, sDstValue);

            sDstValue = EncryptionFunc.Sha512Encrypt(sSrcValue);
            Assert.NotEmpty(sDstValue);

            sDstValue = EncryptionFunc.Sha256Encrypt(sSrcValue);
            Assert.NotEmpty(sDstValue);
        }
    }
}