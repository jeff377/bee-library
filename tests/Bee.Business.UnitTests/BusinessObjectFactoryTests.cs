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
        [DisplayName("CreateBusinessObject 應回傳 SystemBusinessObject 並保留 AccessToken")]
        public void CreateBusinessObject_System_ReturnsSystemBusinessObject()
        {
            var token = Guid.NewGuid();

            var obj = Factory.CreateBusinessObject(token, SysProgIds.System, isLocalCall: true);

            var bo = Assert.IsType<SystemBusinessObject>(obj);
            Assert.Equal(token, bo.AccessToken);
            Assert.True(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("CreateBusinessObject 傳入 isLocalCall=false 應保留設定")]
        public void CreateBusinessObject_System_WithIsLocalCallFalse_PreservesFlag()
        {
            var obj = Factory.CreateBusinessObject(Guid.NewGuid(), SysProgIds.System, isLocalCall: false);

            var bo = Assert.IsType<SystemBusinessObject>(obj);
            Assert.False(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("CreateBusinessObject 應回傳 FormBusinessObject 並保留 ProgId")]
        public void CreateBusinessObject_Form_ReturnsFormBusinessObject()
        {
            var token = Guid.NewGuid();

            var obj = Factory.CreateBusinessObject(token, "prog01", isLocalCall: true);

            var bo = Assert.IsType<FormBusinessObject>(obj);
            Assert.Equal(token, bo.AccessToken);
            Assert.Equal("prog01", bo.ProgId);
            Assert.True(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("CreateBusinessObject 傳入 isLocalCall=false 應保留設定")]
        public void CreateBusinessObject_Form_WithIsLocalCallFalse_PreservesFlag()
        {
            var obj = Factory.CreateBusinessObject(Guid.NewGuid(), "prog01", isLocalCall: false);

            var bo = Assert.IsType<FormBusinessObject>(obj);
            Assert.False(bo.IsLocalCall);
        }
    }
}
