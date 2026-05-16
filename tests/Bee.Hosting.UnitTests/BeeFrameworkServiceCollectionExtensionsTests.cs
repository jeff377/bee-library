using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting.UnitTests
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

        [Fact]
        [DisplayName("AddBeeFramework 解析 IEnterpriseObjectService 應回傳預設實作（觸發 CreateOrDefault 路徑）")]
        public void AddBeeFramework_ResolvesEnterpriseObjectService_ReturnsInstance()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-eos-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                var configuration = new BackendConfiguration();
                var pathOptions = new PathOptions { DefinePath = tempDir };
                services.AddBeeFramework(configuration, pathOptions, autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                var service = sp.GetRequiredService<IEnterpriseObjectService>();

                Assert.NotNull(service);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }
    }
}
