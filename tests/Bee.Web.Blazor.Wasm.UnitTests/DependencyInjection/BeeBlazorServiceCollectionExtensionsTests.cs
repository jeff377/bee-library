using System.ComponentModel;
using Bee.Web.Blazor.Wasm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Web.Blazor.Wasm.UnitTests.DependencyInjection
{
    /// <summary>
    /// Verifies that <c>AddBeeBlazor</c> registers the resolved options and factory
    /// as singletons and forces hosts to declare a remote endpoint (the Wasm RCL
    /// has no in-process backend so the endpoint is mandatory).
    /// </summary>
    public class BeeBlazorServiceCollectionExtensionsTests
    {
        [Fact]
        [DisplayName("AddBeeBlazor + UseRemoteProvider 註冊 options 與 endpoint")]
        public void AddBeeBlazor_UseRemoteProvider_RegistersOptions()
        {
            var services = new ServiceCollection();
            services.AddBeeBlazor(o => o.UseRemoteProvider("http://api.example.com/api"));
            using var sp = services.BuildServiceProvider();

            var options = sp.GetRequiredService<BeeBlazorOptions>();
            Assert.Equal("http://api.example.com/api", options.Endpoint);
        }

        [Fact]
        [DisplayName("AddBeeBlazor 註冊 BeeApiConnectorFactory")]
        public void AddBeeBlazor_RegistersConnectorFactory()
        {
            var services = new ServiceCollection();
            services.AddBeeBlazor(o => o.UseRemoteProvider("http://api.example.com/api"));
            using var sp = services.BuildServiceProvider();

            var factory = sp.GetRequiredService<BeeApiConnectorFactory>();
            Assert.NotNull(factory);
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
            Assert.Throws<ArgumentNullException>(() => services!.AddBeeBlazor(_ => { }));
        }

        [Fact]
        [DisplayName("AddBeeBlazor 對 null configure 拋 ArgumentNullException")]
        public void AddBeeBlazor_NullConfigure_Throws()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() => services.AddBeeBlazor(null!));
        }

        [Fact]
        [DisplayName("Factory.CreateFormConnector 在未設定 endpoint 時拋 InvalidOperationException")]
        public void Factory_NoEndpoint_Throws()
        {
            var factory = new BeeApiConnectorFactory(new BeeBlazorOptions());
            Assert.Throws<InvalidOperationException>(() => factory.CreateFormConnector(Guid.NewGuid(), "Employee"));
        }
    }
}
