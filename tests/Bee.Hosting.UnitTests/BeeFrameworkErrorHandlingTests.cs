using System.ComponentModel;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Definition.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 驗證 <c>AddBeeFramework</c> 在元件型別設定無效時，
    /// 解析服務時能正確拋出 <see cref="InvalidOperationException"/>。
    /// </summary>
    public class BeeFrameworkErrorHandlingTests
    {
        [Fact]
        [DisplayName("AddBeeFramework 設定不存在的 DefineAccess 型別名稱，解析 IDefineAccess 應拋出 InvalidOperationException")]
        public void AddBeeFramework_InvalidDefineAccessTypeName_ThrowsOnResolve()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-err1-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                var config = new BackendConfiguration();
                config.Components.DefineAccess = "NonExistent.DefineAccessType, Bee.Base";
                var pathOptions = new PathOptions { DefinePath = tempDir };
                services.AddBeeFramework(config, pathOptions, autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                Assert.Throws<InvalidOperationException>(() => sp.GetRequiredService<IDefineAccess>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 設定不存在的 DefineStorage 型別名稱，解析 IDefineStorage 應拋出 InvalidOperationException")]
        public void AddBeeFramework_InvalidDefineStorageTypeName_ThrowsOnResolve()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-err2-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                var config = new BackendConfiguration();
                config.Components.DefineStorage = "NonExistent.StorageType, Bee.Base";
                var pathOptions = new PathOptions { DefinePath = tempDir };
                services.AddBeeFramework(config, pathOptions, autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                Assert.Throws<InvalidOperationException>(() => sp.GetRequiredService<IDefineStorage>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 設定不存在的 AccessTokenValidator 型別名稱，解析 IAccessTokenValidator 應拋出 InvalidOperationException")]
        public void AddBeeFramework_InvalidAccessTokenValidatorTypeName_ThrowsOnResolve()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-fw-err3-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                var config = new BackendConfiguration();
                config.Components.AccessTokenValidator = "NonExistent.ValidatorType, Bee.Base";
                var pathOptions = new PathOptions { DefinePath = tempDir };
                services.AddBeeFramework(config, pathOptions, autoCreateMasterKey: true);

                using var sp = services.BuildServiceProvider();
                Assert.Throws<InvalidOperationException>(() => sp.GetRequiredService<IAccessTokenValidator>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { /* best effort */ }
            }
        }
    }
}
