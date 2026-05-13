using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Bee.Api.AspNetCore.UnitTests
{
    /// <summary>
    /// 測試 <see cref="Controllers.ApiServiceController.IsDevelopment"/> 屬性。
    /// 使用自製 <c>FakeServiceProvider</c> 注入 <see cref="IHostEnvironment"/>，
    /// 不需要 BeeTestFixture 或共享的後端 DI 容器。
    /// </summary>
    public class ApiServiceControllerIsDevelopmentTests
    {
        private sealed class FakeHostEnvironment : IHostEnvironment
        {
            public required string EnvironmentName { get; set; }
            public string ApplicationName { get; set; } = string.Empty;
            public string ContentRootPath { get; set; } = string.Empty;
            public IFileProvider ContentRootFileProvider { get; set; } = null!;
        }

        private sealed class FakeServiceProvider : IServiceProvider
        {
            private readonly IHostEnvironment _env;
            public FakeServiceProvider(IHostEnvironment env) => _env = env;
            public object? GetService(Type serviceType) =>
                serviceType == typeof(IHostEnvironment) ? _env : null;
        }

        private sealed class TestableController : Controllers.ApiServiceController
        {
            public bool GetIsDevelopment() => IsDevelopment;
        }

        private static TestableController CreateController(string environmentName)
        {
            var env = new FakeHostEnvironment { EnvironmentName = environmentName };
            var context = new DefaultHttpContext { RequestServices = new FakeServiceProvider(env) };
            return new TestableController { ControllerContext = new ControllerContext { HttpContext = context } };
        }

        [Fact]
        [DisplayName("IsDevelopment 在 Development 環境應回傳 true")]
        public void IsDevelopment_DevelopmentEnvironment_ReturnsTrue()
        {
            var controller = CreateController(Environments.Development);
            Assert.True(controller.GetIsDevelopment());
        }

        [Fact]
        [DisplayName("IsDevelopment 在 Production 環境應回傳 false")]
        public void IsDevelopment_ProductionEnvironment_ReturnsFalse()
        {
            var controller = CreateController(Environments.Production);
            Assert.False(controller.GetIsDevelopment());
        }
    }
}
