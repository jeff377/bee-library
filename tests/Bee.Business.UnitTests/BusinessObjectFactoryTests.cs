using System.ComponentModel;
using Bee.Business.Form;
using Bee.Business.System;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="BusinessObjectFactory"/> 工廠方法測試。
    /// 透過 per-class <see cref="SharedDbFixture"/> 解析 DI-注入後的 factory 實例。
    /// </summary>
    public class BusinessObjectFactoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public BusinessObjectFactoryTests(SharedDbFixture fx) { _fx = fx; }
        private IBusinessObjectFactory Factory => _fx.GetRequiredService<IBusinessObjectFactory>();

        [Fact]
        [DisplayName("CreateSystemBusinessObject 應回傳 SystemBusinessObject 並保留 AccessToken")]
        public void CreateSystemBusinessObject_ReturnsSystemBusinessObject()
        {
            var token = Guid.NewGuid();

            var obj = Factory.CreateSystemBusinessObject(token);

            var bo = Assert.IsType<SystemBusinessObject>(obj);
            Assert.Equal(token, bo.AccessToken);
            Assert.True(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("CreateSystemBusinessObject 傳入 isLocalCall=false 應保留設定")]
        public void CreateSystemBusinessObject_WithIsLocalCallFalse_PreservesFlag()
        {
            var obj = Factory.CreateSystemBusinessObject(Guid.NewGuid(), isLocalCall: false);

            var bo = Assert.IsType<SystemBusinessObject>(obj);
            Assert.False(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("CreateFormBusinessObject 應回傳 FormBusinessObject 並保留 ProgId")]
        public void CreateFormBusinessObject_ReturnsFormBusinessObject()
        {
            var token = Guid.NewGuid();

            var obj = Factory.CreateFormBusinessObject(token, "prog01");

            var bo = Assert.IsType<FormBusinessObject>(obj);
            Assert.Equal(token, bo.AccessToken);
            Assert.Equal("prog01", bo.ProgId);
            Assert.True(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("CreateFormBusinessObject 傳入 isLocalCall=false 應保留設定")]
        public void CreateFormBusinessObject_WithIsLocalCallFalse_PreservesFlag()
        {
            var obj = Factory.CreateFormBusinessObject(Guid.NewGuid(), "prog01", isLocalCall: false);

            var bo = Assert.IsType<FormBusinessObject>(obj);
            Assert.False(bo.IsLocalCall);
        }
    }
}
