using System.ComponentModel;
using Bee.Api.Client.ApiServiceProvider;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 針對 <see cref="LocalApiServiceProvider"/> 建構子與屬性的純邏輯測試。
    /// </summary>
    public class LocalApiServiceProviderTests
    {
        [Fact]
        [DisplayName("LocalApiServiceProvider 建構子應正確設定 AccessToken")]
        public void Constructor_SetsAccessToken()
        {
            var token = Guid.NewGuid();
            var provider = new LocalApiServiceProvider(token);

            Assert.Equal(token, provider.AccessToken);
        }

        [Fact]
        [DisplayName("LocalApiServiceProvider 建構子可接受 Guid.Empty")]
        public void Constructor_EmptyAccessToken_IsAccepted()
        {
            var provider = new LocalApiServiceProvider(Guid.Empty);

            Assert.Equal(Guid.Empty, provider.AccessToken);
        }
    }
}
