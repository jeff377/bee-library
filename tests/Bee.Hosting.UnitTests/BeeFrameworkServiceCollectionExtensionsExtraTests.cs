using System.ComponentModel;
using Bee.Business.Providers;
using Bee.Definition.Security;
using Bee.Tests.Shared;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 補強 <see cref="BeeFrameworkServiceCollectionExtensions"/> 中
    /// DynamicApiEncryptionKeyProvider 路由分支的覆蓋率測試。
    /// </summary>
    public class BeeFrameworkServiceCollectionExtensionsExtraTests
    {
        [Fact]
        [DisplayName("AddBeeFramework 預設組態解析 IApiEncryptionKeyProvider 應回傳 DynamicApiEncryptionKeyProvider")]
        public void AddBeeFramework_DefaultConfig_ResolvesDynamicApiEncryptionKeyProvider()
        {
            // 使用標準 BeeTestFixture（預設 ApiEncryptionKeyProvider = DynamicApiEncryptionKeyProvider）
            // 觸發 CreateApiEncryptionKeyProvider 中 ActivatorUtilities.CreateInstance 分支
            using var fx = new BeeTestFixture();
            var provider = fx.GetRequiredService<IApiEncryptionKeyProvider>();
            Assert.IsType<DynamicApiEncryptionKeyProvider>(provider);
        }
    }
}
