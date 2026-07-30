using System.ComponentModel;
using Bee.Base.Security;

namespace Bee.Base.UnitTests
{
    /// <summary>
    /// ApiKeyHasher 單元測試：salt + SHA-256 的 round-trip 與拒絕情境。
    /// </summary>
    public class ApiKeyHasherTests
    {
        [Fact]
        [DisplayName("HashSecret 產生的雜湊應能被 VerifySecret 驗證通過")]
        public void HashSecret_RoundTrip_Verifies()
        {
            string secret = ApiKeyFormat.CreateSecret();

            string hashed = ApiKeyHasher.HashSecret(secret);

            Assert.True(ApiKeyHasher.VerifySecret(secret, hashed));
        }

        [Fact]
        [DisplayName("HashSecret 對同一 secret 兩次應產生不同雜湊(隨機 salt)")]
        public void HashSecret_SameSecretTwice_ProducesDifferentHashes()
        {
            string secret = ApiKeyFormat.CreateSecret();

            string first = ApiKeyHasher.HashSecret(secret);
            string second = ApiKeyHasher.HashSecret(secret);

            Assert.NotEqual(first, second);
            Assert.True(ApiKeyHasher.VerifySecret(secret, first));
            Assert.True(ApiKeyHasher.VerifySecret(secret, second));
        }

        [Fact]
        [DisplayName("HashSecret 應以 v1. 版本前綴與三段格式儲存")]
        public void HashSecret_UsesVersionedThreePartFormat()
        {
            string hashed = ApiKeyHasher.HashSecret("secret-value");

            Assert.StartsWith("v1.", hashed, StringComparison.Ordinal);
            Assert.Equal(3, hashed.Split('.').Length);
        }

        [Fact]
        [DisplayName("VerifySecret 於 secret 不符時應回傳 false")]
        public void VerifySecret_WrongSecret_ReturnsFalse()
        {
            string hashed = ApiKeyHasher.HashSecret(ApiKeyFormat.CreateSecret());

            Assert.False(ApiKeyHasher.VerifySecret(ApiKeyFormat.CreateSecret(), hashed));
        }

        [Theory]
        [DisplayName("VerifySecret 於儲存格式不合法時應 fail closed")]
        [InlineData("")]
        [InlineData("not-versioned")]
        [InlineData("v1.only-two-parts")]
        [InlineData("v1.!!!notbase64!!!.!!!notbase64!!!")]
        [InlineData("v2.c2FsdA==.aGFzaA==")]
        public void VerifySecret_MalformedStoredHash_ReturnsFalse(string hashedSecret)
        {
            Assert.False(ApiKeyHasher.VerifySecret("any-secret", hashedSecret));
        }

        [Theory]
        [DisplayName("VerifySecret 於 secret 為 null 或空字串時應回傳 false")]
        [InlineData(null)]
        [InlineData("")]
        public void VerifySecret_EmptySecret_ReturnsFalse(string? secret)
        {
            string hashed = ApiKeyHasher.HashSecret("real-secret");

            Assert.False(ApiKeyHasher.VerifySecret(secret, hashed));
        }
    }
}
