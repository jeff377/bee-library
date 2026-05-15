using System.ComponentModel;
using Bee.Business.Providers;
using Bee.Definition;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 驗證 <see cref="BeeFrameworkServiceCollectionExtensions.AddBeeFramework"/> 延遲工廠
    /// 在服務首次解析時正確執行私有建立方法。
    /// </summary>
    public class BeeFrameworkLazyServiceTests
    {
        private static ServiceProvider BuildProvider(string tempDir, Action<BackendConfiguration>? configure = null)
        {
            var configuration = new BackendConfiguration();
            configure?.Invoke(configuration);
            var pathOptions = new PathOptions { DefinePath = tempDir };
            var services = new ServiceCollection();
            services.AddBeeFramework(configuration, pathOptions, autoCreateMasterKey: true);
            return services.BuildServiceProvider();
        }

        [Fact]
        [DisplayName("解析 IApiEncryptionKeyProvider 預設應建立 DynamicApiEncryptionKeyProvider")]
        public void ResolveApiEncryptionKeyProvider_DefaultConfig_ReturnsDynamicProvider()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-lazy-dyn-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                using var sp = BuildProvider(tempDir);
                var provider = sp.GetRequiredService<IApiEncryptionKeyProvider>();
                Assert.IsType<DynamicApiEncryptionKeyProvider>(provider);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        [Fact]
        [DisplayName("解析 IEnterpriseObjectService 應透過 CreateOrDefault 建立實例")]
        public void ResolveEnterpriseObjectService_DefaultConfig_ReturnsInstance()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-lazy-eos-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                using var sp = BuildProvider(tempDir);
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
