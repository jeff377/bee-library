using System.ComponentModel;
using Bee.Business.Providers;
using Bee.Definition;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 驗證 <c>AddBeeFramework</c> 解析 <see cref="IApiEncryptionKeyProvider"/> 的分支選擇。
    /// 預設為 <see cref="DerivedApiEncryptionKeyProvider"/>（session 重建所需），
    /// 未設定 <c>ApiEncryptionKey</c> 時退回以 master key 導出根金鑰。
    /// </summary>
    public class BeeFrameworkApiEncryptionKeyProviderTests
    {
        private static void WithFramework(string prefix, Action<IServiceProvider> assert, Action<BackendConfiguration>? configure = null)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-{prefix}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                var configuration = new BackendConfiguration();
                configure?.Invoke(configuration);
                var pathOptions = new PathOptions { DefinePath = tempDir };

                services.AddBeeFramework(configuration, pathOptions, autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                assert(sp);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 預設組態解析 IApiEncryptionKeyProvider 應回傳 DerivedApiEncryptionKeyProvider")]
        public void AddBeeFramework_DefaultConfig_ResolvesDerivedApiEncryptionKeyProvider()
        {
            WithFramework("derived", sp =>
                Assert.IsType<DerivedApiEncryptionKeyProvider>(sp.GetRequiredService<IApiEncryptionKeyProvider>()));
        }

        [Fact]
        [DisplayName("未設定 ApiEncryptionKey 時仍可導出可用金鑰（以 master key 為根）")]
        public void AddBeeFramework_NoApiEncryptionKey_DerivesUsableKey()
        {
            WithFramework("derived-fallback", sp =>
            {
                var provider = sp.GetRequiredService<IApiEncryptionKeyProvider>();
                var token = Guid.NewGuid();

                var generated = provider.GenerateKeyForLogin(token);

                Assert.Equal(64, generated.Length);
                Assert.Equal(generated, provider.GetKey(token));
            });
        }

        [Fact]
        [DisplayName("明確指定 Dynamic 應解析 DynamicApiEncryptionKeyProvider")]
        public void AddBeeFramework_ConfiguredDynamic_ResolvesDynamicApiEncryptionKeyProvider()
        {
            WithFramework("dynamic",
                sp => Assert.IsType<DynamicApiEncryptionKeyProvider>(sp.GetRequiredService<IApiEncryptionKeyProvider>()),
                configuration => configuration.Components.ApiEncryptionKeyProvider =
                    "Bee.Business.Providers.DynamicApiEncryptionKeyProvider, Bee.Business");
        }

        [Fact]
        [DisplayName("AddBeeFramework 預設組態完整解析服務鏈應不拋例外")]
        public void AddBeeFramework_DefaultConfig_ResolvesServiceChainWithoutException()
        {
            WithFramework("chain", sp =>
            {
                var exception = Record.Exception(() =>
                {
                    _ = sp.GetRequiredService<IApiEncryptionKeyProvider>();
                    _ = sp.GetRequiredService<Bee.Definition.Storage.IDefineAccess>();
                });

                Assert.Null(exception);
            });
        }
    }
}
