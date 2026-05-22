using System.ComponentModel;
using Bee.Web.Blazor.Server.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Web.Blazor.Server.UnitTests.DependencyInjection
{
    /// <summary>
    /// Verifies that <c>AddBeeBlazor</c> registers the resolved options and factory
    /// as singletons, and that the fluent options API picks up the chosen provider.
    /// </summary>
    public class BeeBlazorServiceCollectionExtensionsTests
    {
        [Fact]
        [DisplayName("AddBeeBlazor 不帶 configure 時預設 Local 模式")]
        public void AddBeeBlazor_NoConfigure_DefaultsToLocal()
        {
            var services = new ServiceCollection();
            services.AddBeeBlazor();
            using var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<BeeBlazorOptions>();
            Assert.Equal(BeeBlazorProviderMode.Local, options.Mode);
            Assert.Equal(string.Empty, options.Endpoint);
        }

        [Fact]
        [DisplayName("AddBeeBlazor + UseLocalProvider 維持 Local 模式")]
        public void AddBeeBlazor_UseLocalProvider_StaysLocal()
        {
            var services = new ServiceCollection();
            services.AddBeeBlazor(o => o.UseLocalProvider());
            using var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<BeeBlazorOptions>();
            Assert.Equal(BeeBlazorProviderMode.Local, options.Mode);
        }

        [Fact]
        [DisplayName("AddBeeBlazor + UseRemoteProvider 切換為 Remote 模式並保留 endpoint")]
        public void AddBeeBlazor_UseRemoteProvider_SwitchesToRemote()
        {
            var services = new ServiceCollection();
            services.AddBeeBlazor(o => o.UseRemoteProvider("http://example.com/api"));
            using var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<BeeBlazorOptions>();
            Assert.Equal(BeeBlazorProviderMode.Remote, options.Mode);
            Assert.Equal("http://example.com/api", options.Endpoint);
        }

        [Fact]
        [DisplayName("AddBeeBlazor 註冊 BeeApiConnectorFactory")]
        public void AddBeeBlazor_RegistersConnectorFactory()
        {
            var services = new ServiceCollection();
            services.AddBeeBlazor();
            using var sp = services.BuildServiceProvider();

            var factory = sp.GetRequiredService<BeeApiConnectorFactory>();
            Assert.Equal(BeeBlazorProviderMode.Local, factory.Mode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("UseRemoteProvider 拒絕空 endpoint")]
        public void UseRemoteProvider_EmptyEndpoint_Throws(string? endpoint)
        {
            var options = new BeeBlazorOptions();
            Assert.ThrowsAny<ArgumentException>(() => options.UseRemoteProvider(endpoint!));
        }

        [Fact]
        [DisplayName("AddBeeBlazor 對 null services 拋 ArgumentNullException")]
        public void AddBeeBlazor_NullServices_Throws()
        {
            IServiceCollection? services = null;
            Assert.Throws<ArgumentNullException>(() => services!.AddBeeBlazor());
        }
    }
}
