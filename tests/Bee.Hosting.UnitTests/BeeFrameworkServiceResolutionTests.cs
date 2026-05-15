using System.ComponentModel;
using Bee.Business.Providers;
using Bee.Definition;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting.UnitTests
{
    public class BeeFrameworkServiceResolutionTests
    {
        [Fact]
        [DisplayName("AddBeeFramework 預設設定解析 IApiEncryptionKeyProvider 應回傳 DynamicApiEncryptionKeyProvider")]
        public void AddBeeFramework_DefaultConfig_ResolvesDynamicApiEncryptionKeyProvider()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-dyn-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                var pathOptions = new PathOptions { DefinePath = tempDir };
                services.AddBeeFramework(new BackendConfiguration(), pathOptions, autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                var provider = sp.GetRequiredService<IApiEncryptionKeyProvider>();

                Assert.IsType<DynamicApiEncryptionKeyProvider>(provider);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 預設設定解析 IEnterpriseObjectService 應回傳有效實例")]
        public void AddBeeFramework_DefaultConfig_ResolvesEnterpriseObjectService()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-eos-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                var pathOptions = new PathOptions { DefinePath = tempDir };
                services.AddBeeFramework(new BackendConfiguration(), pathOptions, autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                var eos = sp.GetRequiredService<IEnterpriseObjectService>();

                Assert.NotNull(eos);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }
    }
}
