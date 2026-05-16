using System.ComponentModel;
using Bee.Business.Providers;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 驗證 <c>AddBeeFramework</c> 以預設組態解析 <see cref="IApiEncryptionKeyProvider"/> 時
    /// 走動態提供者（<see cref="DynamicApiEncryptionKeyProvider"/>）分支。
    /// </summary>
    public class BeeFrameworkDynamicProviderTests
    {
        [Fact]
        [DisplayName("AddBeeFramework 預設組態解析 IApiEncryptionKeyProvider 應回傳 DynamicApiEncryptionKeyProvider")]
        public void AddBeeFramework_DefaultConfig_ResolvesDynamicApiEncryptionKeyProvider()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-dynamic-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                var configuration = new BackendConfiguration();
                var pathOptions = new PathOptions { DefinePath = tempDir };

                services.AddBeeFramework(configuration, pathOptions, autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                var provider = sp.GetRequiredService<IApiEncryptionKeyProvider>();

                Assert.IsType<DynamicApiEncryptionKeyProvider>(provider);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 預設組態完整解析服務鏈應不拋例外")]
        public void AddBeeFramework_DefaultConfig_ResolvesServiceChainWithoutException()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-chain-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                var configuration = new BackendConfiguration();
                var pathOptions = new PathOptions { DefinePath = tempDir };
                services.AddBeeFramework(configuration, pathOptions, autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                var exception = Record.Exception(() =>
                {
                    _ = sp.GetRequiredService<IApiEncryptionKeyProvider>();
                    _ = sp.GetRequiredService<Bee.Definition.Storage.IDefineAccess>();
                });

                Assert.Null(exception);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }
    }
}
