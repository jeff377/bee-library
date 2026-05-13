using System.ComponentModel;
using Bee.Api.AspNetCore.Bootstrapping;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Api.AspNetCore.UnitTests
{
    public class BeeFrameworkApplicationBuilderExtensionsTests
    {
        private sealed class FakeCacheBootstrapper : ICacheBootstrapper { }
        private sealed class FakeDbConnectionManagerBootstrapper : IDbConnectionManagerBootstrapper { }

        private sealed class FakeApplicationBuilder : IApplicationBuilder
        {
            public IServiceProvider ApplicationServices { get; set; } = null!;
            public IFeatureCollection ServerFeatures => null!;
            public IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();
            public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware) => this;
            public IApplicationBuilder New() => this;
            public RequestDelegate Build() => _ => Task.CompletedTask;
        }

        [Fact]
        [DisplayName("UseBeeFramework 傳入 null 應拋出 ArgumentNullException")]
        public void UseBeeFramework_NullApp_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BeeFrameworkApplicationBuilderExtensions.UseBeeFramework(null!));
        }

        [Fact]
        [DisplayName("UseBeeFramework 應解析 bootstrapper 並回傳相同 IApplicationBuilder 實例")]
        public void UseBeeFramework_ValidApp_ResolvesBootstrappersAndReturnsSameApp()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ICacheBootstrapper, FakeCacheBootstrapper>();
            services.AddSingleton<IDbConnectionManagerBootstrapper, FakeDbConnectionManagerBootstrapper>();
            var provider = services.BuildServiceProvider();

            var app = new FakeApplicationBuilder { ApplicationServices = provider };
            var result = app.UseBeeFramework();

            Assert.Same(app, result);
        }
    }
}
