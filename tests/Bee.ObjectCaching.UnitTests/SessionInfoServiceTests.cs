using System.ComponentModel;
using Bee.Definition;
using Bee.ObjectCaching.Services;
using Bee.Definition.Identity;

namespace Bee.ObjectCaching.UnitTests
{
    /// <summary>
    /// <see cref="SessionInfoService"/> 行為測試。每個測試自建獨立的
    /// <see cref="CacheContainerService"/>（不共用 process-wide cache），
    /// 不依賴 <see cref="DefinePathInfo"/>，可與其他 test class 平行執行。
    /// </summary>
    public class SessionInfoServiceTests
    {
        private static SessionInfoService NewService()
            => new SessionInfoService(new CacheContainerService(new Bee.Definition.Storage.FileDefineStorage(
                new PathOptions { DefinePath = Path.GetTempPath() })));

        [Fact]
        [DisplayName("Set/Get/Remove 流程應正確操作 Session 快取")]
        public void Set_Get_Remove_Flow_Works()
        {
            var service = NewService();
            var token = Guid.NewGuid();
            var info = new SessionInfo
            {
                AccessToken = token,
                UserId = "svc_user",
                UserName = "Service User"
            };

            service.Set(info);
            var loaded = service.Get(token);
            Assert.NotNull(loaded);
            Assert.Equal(token, loaded.AccessToken);
            Assert.Equal("svc_user", loaded.UserId);

            service.Remove(token);
            Assert.Null(service.Get(token));
        }

        [Fact]
        [DisplayName("Get 不存在的 token 應回傳 null")]
        public void Get_MissingToken_ReturnsNull()
        {
            var service = NewService();
            Assert.Null(service.Get(Guid.NewGuid()));
        }

        [Fact]
        [DisplayName("EnterpriseObjectService 可被建立並符合介面")]
        public void EnterpriseObjectService_CanBeInstantiated()
        {
            var service = new EnterpriseObjectService();
            Assert.IsType<IEnterpriseObjectService>(service, exactMatch: false);
        }
    }
}
