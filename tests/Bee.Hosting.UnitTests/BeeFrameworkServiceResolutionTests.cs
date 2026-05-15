using System.ComponentModel;
using Bee.Business;
using Bee.Definition;
using Bee.Definition.Identity;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Hosting.UnitTests
{
    public class BeeFrameworkServiceResolutionTests
    {
        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 IFormRepositoryFactory")]
        public void AddBeeFramework_CanResolve_IFormRepositoryFactory()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-res-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                services.AddBeeFramework(new BackendConfiguration(), new PathOptions { DefinePath = tempDir }, autoCreateMasterKey: true);
                using var sp = services.BuildServiceProvider();
                Assert.NotNull(sp.GetRequiredService<IFormRepositoryFactory>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 ISystemRepositoryFactory")]
        public void AddBeeFramework_CanResolve_ISystemRepositoryFactory()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-res-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                services.AddBeeFramework(new BackendConfiguration(), new PathOptions { DefinePath = tempDir }, autoCreateMasterKey: true);
                using var sp = services.BuildServiceProvider();
                Assert.NotNull(sp.GetRequiredService<ISystemRepositoryFactory>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 IBusinessObjectFactory")]
        public void AddBeeFramework_CanResolve_IBusinessObjectFactory()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-res-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                services.AddBeeFramework(new BackendConfiguration(), new PathOptions { DefinePath = tempDir }, autoCreateMasterKey: true);
                using var sp = services.BuildServiceProvider();
                Assert.NotNull(sp.GetRequiredService<IBusinessObjectFactory>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 ISessionInfoService")]
        public void AddBeeFramework_CanResolve_ISessionInfoService()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-res-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                services.AddBeeFramework(new BackendConfiguration(), new PathOptions { DefinePath = tempDir }, autoCreateMasterKey: true);
                using var sp = services.BuildServiceProvider();
                Assert.NotNull(sp.GetRequiredService<ISessionInfoService>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 IAccessTokenValidator")]
        public void AddBeeFramework_CanResolve_IAccessTokenValidator()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-res-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                services.AddBeeFramework(new BackendConfiguration(), new PathOptions { DefinePath = tempDir }, autoCreateMasterKey: true);
                using var sp = services.BuildServiceProvider();
                Assert.NotNull(sp.GetRequiredService<IAccessTokenValidator>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 IFormBoTypeResolver")]
        public void AddBeeFramework_CanResolve_IFormBoTypeResolver()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-res-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                services.AddBeeFramework(new BackendConfiguration(), new PathOptions { DefinePath = tempDir }, autoCreateMasterKey: true);
                using var sp = services.BuildServiceProvider();
                Assert.NotNull(sp.GetRequiredService<IFormBoTypeResolver>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }

        [Fact]
        [DisplayName("AddBeeFramework 建立的容器應能解析 IEnterpriseObjectService")]
        public void AddBeeFramework_CanResolve_IEnterpriseObjectService()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bee-res-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var services = new ServiceCollection();
                services.AddBeeFramework(new BackendConfiguration(), new PathOptions { DefinePath = tempDir }, autoCreateMasterKey: true);
                using var sp = services.BuildServiceProvider();
                Assert.NotNull(sp.GetRequiredService<IEnterpriseObjectService>());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch (IOException) { }
            }
        }
    }
}
