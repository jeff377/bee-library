using System.ComponentModel;
using Bee.Definition;
using Bee.ObjectCaching.Services;

namespace Bee.ObjectCaching.UnitTests
{
    [Collection("Initialize")]
    public class SessionInfoServiceTests
    {
        [Fact]
        [DisplayName("Set/Get/Remove 流程應正確操作 Session 快取")]
        public void Set_Get_Remove_Flow_Works()
        {
            var service = new SessionInfoService();
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
            var service = new SessionInfoService();
            Assert.Null(service.Get(Guid.NewGuid()));
        }

        [Fact]
        [DisplayName("EnterpriseObjectService 可被建立並符合介面")]
        public void EnterpriseObjectService_CanBeInstantiated()
        {
            var service = new EnterpriseObjectService();
            Assert.IsAssignableFrom<IEnterpriseObjectService>(service);
        }
    }
}
