using System.ComponentModel;
using Bee.Definition;
using Bee.Definition.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting.UnitTests
{
    public class BeeFrameworkIntegrationTests
    {
        [Fact]
        [DisplayName("AddBeeFramework 傳入有效組態且 autoCreateMasterKey=true 應完成服務注冊並回傳 IServiceCollection")]
        public void AddBeeFramework_ValidConfigurationAutoCreateKey_RegistersServices()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                var configuration = new BackendConfiguration();
                var pathOptions = new PathOptions { DefinePath = tempDir };

                // autoCreateMasterKey=true → Master.key 自動建立於 tempDir
                // 同時涵蓋 DecryptSecurityKeys、CacheInfo.Initialize 及所有 AddSingleton 注冊路徑
                var result = services.AddBeeFramework(configuration, pathOptions, autoCreateMasterKey: true);

                Assert.Same(services, result);
                Assert.True(services.Count > 0);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }
    }
}
