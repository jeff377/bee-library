using System.ComponentModel;
using Bee.Business.AuditLog;
using Bee.Business.Form;
using Bee.Business.System;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="BusinessObjectFactoryExtensions"/> 擴充方法測試。
    /// 驗證 typed wrapper 直接回傳介面、避免呼叫端重複 cast。
    /// </summary>
    public class BusinessObjectFactoryExtensionsTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public BusinessObjectFactoryExtensionsTests(SharedDbFixture fx) { _fx = fx; }

        private IBusinessObjectFactory Factory => _fx.GetRequiredService<IBusinessObjectFactory>();

        [Fact]
        [DisplayName("CreateFormBO 應回傳 IFormBusinessObject 介面實例")]
        public void CreateFormBO_ReturnsFormBusinessObjectInterface()
        {
            var token = Guid.NewGuid();

            IFormBusinessObject bo = Factory.CreateFormBO(token, "prog01", isLocalCall: true);

            Assert.NotNull(bo);
            Assert.IsType<FormBusinessObject>(bo);
        }

        [Fact]
        [DisplayName("CreateFormBO 傳入 isLocalCall=false 應保留設定")]
        public void CreateFormBO_WithIsLocalCallFalse_PreservesFlag()
        {
            var bo = (FormBusinessObject)Factory.CreateFormBO(Guid.NewGuid(), "prog01", isLocalCall: false);

            Assert.False(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("CreateSystemBO 應回傳 ISystemBusinessObject 介面實例")]
        public void CreateSystemBO_ReturnsSystemBusinessObjectInterface()
        {
            var token = Guid.NewGuid();

            ISystemBusinessObject bo = Factory.CreateSystemBO(token, isLocalCall: true);

            Assert.NotNull(bo);
            Assert.IsType<SystemBusinessObject>(bo);
        }

        [Fact]
        [DisplayName("CreateSystemBO 傳入 isLocalCall=false 應保留設定")]
        public void CreateSystemBO_WithIsLocalCallFalse_PreservesFlag()
        {
            var bo = (SystemBusinessObject)Factory.CreateSystemBO(Guid.NewGuid(), isLocalCall: false);

            Assert.False(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("CreateLogBO 應回傳 ILogBusinessObject 介面實例")]
        public void CreateLogBO_ReturnsLogBusinessObjectInterface()
        {
            var token = Guid.NewGuid();

            ILogBusinessObject bo = Factory.CreateLogBO(token, isLocalCall: true);

            Assert.NotNull(bo);
            Assert.IsType<LogBusinessObject>(bo);
        }

        [Fact]
        [DisplayName("CreateLogBO 傳入 isLocalCall=false 應保留設定")]
        public void CreateLogBO_WithIsLocalCallFalse_PreservesFlag()
        {
            var bo = (LogBusinessObject)Factory.CreateLogBO(Guid.NewGuid(), isLocalCall: false);

            Assert.False(bo.IsLocalCall);
        }

        [Fact]
        [DisplayName("CreateLogBO 應解析到 AuditLog 這個 progId，而非 System")]
        public void CreateLogBO_ResolvesTheAuditLogProgId()
        {
            // 三個 CreateXxxBO 的差別只在寫死的 progId；抄錯會回傳一個型別轉換剛好也過的 BO。
            var bo = (LogBusinessObject)Factory.CreateLogBO(Guid.NewGuid(), isLocalCall: true);

            Assert.Equal(SysProgIds.AuditLog, bo.ProgId);
        }

        [Fact]
        [DisplayName("CreateLogBO factory 為 null 應拋 ArgumentNullException")]
        public void CreateLogBO_NullFactory_Throws()
        {
            IBusinessObjectFactory? factory = null;
            Assert.Throws<ArgumentNullException>(() => factory!.CreateLogBO(Guid.NewGuid(), isLocalCall: true));
        }

        [Fact]
        [DisplayName("CreateFormBO factory 為 null 應拋 ArgumentNullException")]
        public void CreateFormBO_NullFactory_Throws()
        {
            IBusinessObjectFactory? factory = null;
            Assert.Throws<ArgumentNullException>(() => factory!.CreateFormBO(Guid.NewGuid(), "prog01", isLocalCall: true));
        }

        [Fact]
        [DisplayName("CreateSystemBO factory 為 null 應拋 ArgumentNullException")]
        public void CreateSystemBO_NullFactory_Throws()
        {
            IBusinessObjectFactory? factory = null;
            Assert.Throws<ArgumentNullException>(() => factory!.CreateSystemBO(Guid.NewGuid(), isLocalCall: true));
        }
    }
}
