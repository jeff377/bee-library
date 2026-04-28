using System.ComponentModel;
using Bee.Business.Form;
using Bee.Business.System;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="BusinessObjectFactory"/> 工廠方法測試。
    /// </summary>
    public class BusinessObjectFactoryTests
    {
        [Fact]
        [DisplayName("CreateSystemBusinessObject 應回傳 SystemBusinessObject 並保留 AccessToken")]
        public void CreateSystemBusinessObject_ReturnsSystemBusinessObject()
        {
            var provider = new BusinessObjectFactory();
            var token = Guid.NewGuid();

            var obj = provider.CreateSystemBusinessObject(token);

            var bo = Assert.IsType<SystemBusinessObject>(obj);
            Assert.Equal(token, bo.AccessToken);
            Assert.True(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("CreateSystemBusinessObject 傳入 isLocalCall=false 應保留設定")]
        public void CreateSystemBusinessObject_WithIsLocalCallFalse_PreservesFlag()
        {
            var provider = new BusinessObjectFactory();

            var obj = provider.CreateSystemBusinessObject(Guid.NewGuid(), isLocalCall: false);

            var bo = Assert.IsType<SystemBusinessObject>(obj);
            Assert.False(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("CreateFormBusinessObject 應回傳 FormBusinessObject 並保留 ProgId")]
        public void CreateFormBusinessObject_ReturnsFormBusinessObject()
        {
            var provider = new BusinessObjectFactory();
            var token = Guid.NewGuid();

            var obj = provider.CreateFormBusinessObject(token, "prog01");

            var bo = Assert.IsType<FormBusinessObject>(obj);
            Assert.Equal(token, bo.AccessToken);
            Assert.Equal("prog01", bo.ProgId);
            Assert.True(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("CreateFormBusinessObject 傳入 isLocalCall=false 應保留設定")]
        public void CreateFormBusinessObject_WithIsLocalCallFalse_PreservesFlag()
        {
            var provider = new BusinessObjectFactory();

            var obj = provider.CreateFormBusinessObject(Guid.NewGuid(), "prog01", isLocalCall: false);

            var bo = Assert.IsType<FormBusinessObject>(obj);
            Assert.False(bo.IsLocalCall);
        }
    }
}
