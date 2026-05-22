using System.ComponentModel;
using Bee.Api.Client.Providers;
using Bee.Web.Blazor.Server.DependencyInjection;

namespace Bee.Web.Blazor.Server.UnitTests.DependencyInjection
{
    /// <summary>
    /// Verifies <see cref="BeeApiConnectorFactory"/> emits the right connector
    /// flavour (Local vs Remote) per the supplied <see cref="BeeBlazorOptions"/>.
    /// </summary>
    public class BeeApiConnectorFactoryTests
    {
        [Fact]
        [DisplayName("Local 模式 CreateFormConnector 使用 LocalApiProvider")]
        public void Local_CreateFormConnector_UsesLocalProvider()
        {
            var factory = new BeeApiConnectorFactory(new BeeBlazorOptions().UseLocalProvider());

            var connector = factory.CreateFormConnector(Guid.NewGuid(), "Employee");

            Assert.IsType<LocalApiProvider>(connector.Provider);
            Assert.Equal("Employee", connector.ProgId);
        }

        [Fact]
        [DisplayName("Remote 模式 CreateFormConnector 使用 RemoteApiProvider 並保留 ProgId")]
        public void Remote_CreateFormConnector_UsesRemoteProvider()
        {
            var options = new BeeBlazorOptions().UseRemoteProvider("http://api.example.com/api");
            var factory = new BeeApiConnectorFactory(options);

            var connector = factory.CreateFormConnector(Guid.NewGuid(), "Employee");

            Assert.IsType<RemoteApiProvider>(connector.Provider);
            Assert.Equal("Employee", connector.ProgId);
        }

        [Fact]
        [DisplayName("Local 模式 CreateSystemConnector 使用 LocalApiProvider")]
        public void Local_CreateSystemConnector_UsesLocalProvider()
        {
            var factory = new BeeApiConnectorFactory(new BeeBlazorOptions());

            var connector = factory.CreateSystemConnector(Guid.NewGuid());

            Assert.IsType<LocalApiProvider>(connector.Provider);
        }

        [Fact]
        [DisplayName("Remote 模式 CreateSystemConnector 使用 RemoteApiProvider")]
        public void Remote_CreateSystemConnector_UsesRemoteProvider()
        {
            var options = new BeeBlazorOptions().UseRemoteProvider("http://api.example.com/api");
            var factory = new BeeApiConnectorFactory(options);

            var connector = factory.CreateSystemConnector(Guid.NewGuid());

            Assert.IsType<RemoteApiProvider>(connector.Provider);
        }

        [Fact]
        [DisplayName("CreateFormConnector 對空白 progId 拋 ArgumentException")]
        public void CreateFormConnector_BlankProgId_Throws()
        {
            var factory = new BeeApiConnectorFactory(new BeeBlazorOptions());
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
