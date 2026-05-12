using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Api.AspNetCore.UnitTests
{
    public class BeeFrameworkServiceCollectionExtensionsTests
    {
        [Fact]
        [DisplayName("AddBeeFramework 傳入 null services 應拋出 ArgumentNullException")]
        public void AddBeeFramework_NullServices_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                BeeFrameworkServiceCollectionExtensions.AddBeeFramework(
                    null!, new BackendConfiguration(), new PathOptions()));
        }

        [Fact]
        [DisplayName("AddBeeFramework 傳入 null configuration 應拋出 ArgumentNullException")]
        public void AddBeeFramework_NullConfiguration_ThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() =>
                services.AddBeeFramework(null!, new PathOptions()));
        }

        [Fact]
        [DisplayName("AddBeeFramework 傳入 null pathOptions 應拋出 ArgumentNullException")]
        public void AddBeeFramework_NullPathOptions_ThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() =>
                services.AddBeeFramework(new BackendConfiguration(), null!));
        }
    }
}
