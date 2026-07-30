using System.ComponentModel;
using Bee.Base.Security;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// ApiKeyFormat 單元測試：{sysId}.{secret} 兩段式格式的組成、解析與 sys_id 字元集驗證。
    /// </summary>
    public class ApiKeyFormatTests
    {
        [Fact]
        [DisplayName("CreateSecret 應產生 URL-safe 且每次不同的 256-bit secret")]
        public void CreateSecret_ProducesUrlSafeUniqueValues()
        {
            string first = ApiKeyFormat.CreateSecret();
            string second = ApiKeyFormat.CreateSecret();

            Assert.NotEqual(first, second);
            // 43 characters is the base64url length of 32 bytes without padding.
            Assert.Equal(43, first.Length);
            Assert.DoesNotContain('+', first);
            Assert.DoesNotContain('/', first);
            Assert.DoesNotContain('=', first);
            Assert.DoesNotContain(ApiKeyFormat.Separator, first);
        }

        [Fact]
        [DisplayName("Compose 後 TryParse 應還原原本的兩段內容")]
        public void Compose_ThenTryParse_RoundTrips()
        {
            string secret = ApiKeyFormat.CreateSecret();
            string key = ApiKeyFormat.Compose("northwind-desktop", secret);

            bool parsed = ApiKeyFormat.TryParse(key, out string sysId, out string parsedSecret);

            Assert.True(parsed);
            Assert.Equal("northwind-desktop", sysId);
            Assert.Equal(secret, parsedSecret);
        }

        [Theory]
        [DisplayName("IsValidSysId 應接受合法的識別碼")]
        [InlineData("abc")]
        [InlineData("northwind-desktop")]
        [InlineData("vendor-x-2026")]
        public void IsValidSysId_ValidValues_ReturnsTrue(string sysId)
        {
            Assert.True(ApiKeyFormat.IsValidSysId(sysId));
        }

        [Theory]
        [DisplayName("IsValidSysId 應拒絕不合法的識別碼")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ab")]                    // 短於下限
        [InlineData("-leading")]              // 開頭連字號
        [InlineData("trailing-")]             // 結尾連字號
        [InlineData("Has-Upper")]             // 大寫
        [InlineData("has.dot")]               // 含分隔字元，會讓切段有歧義
        [InlineData("has_underscore")]
        [InlineData("has space")]
        public void IsValidSysId_InvalidValues_ReturnsFalse(string? sysId)
        {
            Assert.False(ApiKeyFormat.IsValidSysId(sysId));
        }

        [Fact]
        [DisplayName("IsValidSysId 應拒絕超過長度上限的識別碼")]
        public void IsValidSysId_TooLong_ReturnsFalse()
        {
            string sysId = new string('a', ApiKeyFormat.MaxSysIdLength + 1);

            Assert.False(ApiKeyFormat.IsValidSysId(sysId));
        }

        [Theory]
        [DisplayName("TryParse 於格式不符時應回傳 false 且不產出任何片段")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("no-separator")]
        [InlineData(".leading-separator")]
        [InlineData("trailing-separator.")]
        [InlineData("Bad-SysId.secret")]
        public void TryParse_Malformed_ReturnsFalse(string? key)
        {
            bool parsed = ApiKeyFormat.TryParse(key, out string sysId, out string secret);

            Assert.False(parsed);
            Assert.Equal(string.Empty, sysId);
            Assert.Equal(string.Empty, secret);
        }

        [Fact]
        [DisplayName("TryParse 應以第一個分隔字元切段，secret 內的分隔字元不影響切法")]
        public void TryParse_SplitsOnFirstSeparator()
        {
            bool parsed = ApiKeyFormat.TryParse("app-id.secret.with.dots", out string sysId, out string secret);

            Assert.True(parsed);
            Assert.Equal("app-id", sysId);
            Assert.Equal("secret.with.dots", secret);
        }
    }
}
