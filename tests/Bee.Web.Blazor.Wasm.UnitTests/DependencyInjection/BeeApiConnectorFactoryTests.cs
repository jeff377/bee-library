using System.ComponentModel;
using Bee.Api.Client.Providers;
using Bee.Web.Blazor.Wasm.DependencyInjection;

namespace Bee.Web.Blazor.Wasm.UnitTests.DependencyInjection
{
    /// <summary>
    /// Verifies <see cref="BeeApiConnectorFactory"/> always returns
    /// <see cref="RemoteApiProvider"/>-backed connectors (Wasm cannot host an
    /// in-process backend).
    /// </summary>
    public class BeeApiConnectorFactoryTests
    {
        private const string Endpoint = "http://api.example.com/api";

        [Fact]
        [DisplayName("CreateFormConnector 使用 RemoteApiProvider 並保留 ProgId")]
        public void CreateFormConnector_UsesRemoteProvider()
        {
            var options = new BeeBlazorOptions().UseRemoteProvider(Endpoint);
            var factory = new BeeApiConnectorFactory(options);

            var connector = factory.CreateFormConnector(Guid.NewGuid(), "Employee");

            Assert.IsType<RemoteApiProvider>(connector.Provider);
            Assert.Equal("Employee", connector.ProgId);
        }

        [Fact]
        [DisplayName("CreateSystemConnector 使用 RemoteApiProvider")]
        public void CreateSystemConnector_UsesRemoteProvider()
        {
            var options = new BeeBlazorOptions().UseRemoteProvider(Endpoint);
            var factory = new BeeApiConnectorFactory(options);

            var connector = factory.CreateSystemConnector(Guid.NewGuid());

            Assert.IsType<RemoteApiProvider>(connector.Provider);
        }

        [Fact]
        [DisplayName("CreateFormConnector 對空白 progId 拋 ArgumentException")]
        public void CreateFormConnector_BlankProgId_Throws()
        {
            var factory = new BeeApiConnectorFactory(new BeeBlazorOptions().UseRemoteProvider(Endpoint));
            Assert.Throws<ArgumentException>(() => factory.CreateFormConnector(Guid.NewGuid(), "  "));
        }

        [Fact]
        [DisplayName("BeeApiConnectorFactory 對 null options 拋 ArgumentNullException")]
        public void Constructor_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new BeeApiConnectorFactory(null!));
        }
    }
}
