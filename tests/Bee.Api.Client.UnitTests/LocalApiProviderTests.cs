using System.ComponentModel;
using Bee.Api.Client.Providers;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 針對 <see cref="LocalApiProvider"/> 建構子與屬性的純邏輯測試。
    /// </summary>
    public class LocalApiProviderTests
    {
        [Fact]
        [DisplayName("LocalApiProvider 建構子應正確設定 AccessToken")]
        public void Constructor_SetsAccessToken()
        {
            var token = Guid.NewGuid();
            var provider = new LocalApiProvider(token);

            Assert.Equal(token, provider.AccessToken);
        }

        [Fact]
        [DisplayName("LocalApiProvider 建構子可接受 Guid.Empty")]
        public void Constructor_EmptyAccessToken_IsAccepted()
        {
            var provider = new LocalApiProvider(Guid.Empty);

            Assert.Equal(Guid.Empty, provider.AccessToken);
        }
    }
}
