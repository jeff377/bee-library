using System.ComponentModel;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting.UnitTests
{
    public class BeeFrameworkServiceResolutionTests
    {
        [Fact]
        [DisplayName("AddBeeFramework 預設組態解析 IDefineStorage 及 IDefineAccess 應各回傳非 null")]
        public void AddBeeFramework_DefaultConfiguration_DefineServicesAreResolvable()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-res-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                services.AddBeeFramework(
                    new BackendConfiguration(),
                    new PathOptions { DefinePath = tempDir },
                    autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();

                // 驗證工廠 lambda 執行不拋出例外（覆蓋 CreateDefineStorage 與 ResolveDefineAccess 方法體）
                var ex = Record.Exception(() =>
                {
                    _ = sp.GetRequiredService<IDefineStorage>();
                    _ = sp.GetRequiredService<IDefineAccess>();
                });
                Assert.Null(ex);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 設定不存在的 DefineAccess 型別，解析 IDefineAccess 時應拋出例外")]
        public void AddBeeFramework_InvalidDefineAccessType_ThrowsOnResolution()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-res-inv-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var config = new BackendConfiguration();
                config.Components.DefineAccess = "Bee.Base.TypeNotExistXxxPlaceholder, Bee.Base";
                var services = new ServiceCollection();
                services.AddBeeFramework(
                    config,
                    new PathOptions { DefinePath = tempDir },
                    autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                var ex = Record.Exception(() => sp.GetRequiredService<IDefineAccess>());

                Assert.NotNull(ex);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 設定靜態類別為 AccessTokenValidator，解析服務時應拋出例外")]
        public void AddBeeFramework_StaticClassAsAccessTokenValidator_ThrowsOnResolution()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-res-static-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var config = new BackendConfiguration();
                // AssemblyLoader 為 static class，無法被 ActivatorUtilities 或 AssemblyLoader.CreateInstance 實體化，
                // 會觸發 CreateConfigurableService 的 catch(InvalidOperationException) 分支及後續 ?? throw
                config.Components.AccessTokenValidator = "Bee.Base.AssemblyLoader, Bee.Base";
                var services = new ServiceCollection();
                services.AddBeeFramework(
                    config,
                    new PathOptions { DefinePath = tempDir },
                    autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                var ex = Record.Exception(() => sp.GetRequiredService<IAccessTokenValidator>());

                Assert.NotNull(ex);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }
    }
}
