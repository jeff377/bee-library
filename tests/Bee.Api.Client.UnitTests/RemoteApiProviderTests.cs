using System.ComponentModel;
using Bee.Api.Client.Providers;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 針對 <see cref="RemoteApiProvider"/> 建構子與屬性的純邏輯測試。
    /// </summary>
    public class RemoteApiProviderTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("RemoteApiProvider 建構子空白 endpoint 應拋 ArgumentException")]
        public void Constructor_NullOrEmptyEndpoint_ThrowsArgumentException(string? endpoint)
        {
            Assert.Throws<ArgumentException>(() => new RemoteApiProvider(endpoint!, Guid.Empty));
        }

        [Fact]
        [DisplayName("RemoteApiProvider 建構子應正確設定 Endpoint 與 AccessToken")]
        public void Constructor_ValidArgs_SetsProperties()
        {
            var token = Guid.NewGuid();
            var provider = new RemoteApiProvider("http://example.com/api", token);

            Assert.Equal("http://example.com/api", provider.Endpoint);
            Assert.Equal(token, provider.AccessToken);
        }

        [Fact]
        [DisplayName("RemoteApiProvider 建構子可接受 Guid.Empty 作為 AccessToken（用於 Login/Ping）")]
        public void Constructor_EmptyAccessToken_IsAccepted()
        {
            var provider = new RemoteApiProvider("http://example.com/api", Guid.Empty);

            Assert.Equal(Guid.Empty, provider.AccessToken);
        }
    }
}
