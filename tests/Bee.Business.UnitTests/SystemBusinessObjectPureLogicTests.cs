using System.ComponentModel;
using Bee.Business.System;
using Bee.Definition;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject"/> 不依賴 Repository / DefineAccess 的純邏輯分支測試。
    /// Phase 4 之後 BO ctor 需要 IBeeContext，透過 <see cref="TestBeeContext.Create(BeeTestFixture)"/>
    /// 從 per-class fixture 取得 DI 服務。
    /// </summary>
    public class SystemBusinessObjectPureLogicTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemBusinessObjectPureLogicTests(SharedDbFixture fx) { _fx = fx; }
        [Fact]
        [DisplayName("Ping 應回傳 Status=ok、回應 TraceId 與 UTC ServerTime")]
        public void Ping_ReturnsExpectedValues()
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);
            var args = new PingArgs { ClientName = "client01", TraceId = "trace-xyz" };
            var before = DateTime.UtcNow.AddSeconds(-1);

            var result = bo.Ping(args);

            Assert.Equal("ok", result.Status);
            Assert.Equal("trace-xyz", result.TraceId);
            Assert.True(result.ServerTime >= before);
            Assert.True(result.ServerTime <= DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        [DisplayName("CheckPackageUpdate 在基底類別應拋 NotSupportedException")]
        public void CheckPackageUpdate_BaseClass_ThrowsNotSupported()
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);
            Assert.Throws<NotSupportedException>(() => bo.CheckPackageUpdate(new CheckPackageUpdateArgs()));
        }

        [Fact]
        [DisplayName("GetPackage 在基底類別應拋 NotSupportedException")]
        public void GetPackage_BaseClass_ThrowsNotSupported()
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);
            Assert.Throws<NotSupportedException>(() => bo.GetPackage(new GetPackageArgs()));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(86401)]
        [DisplayName("CreateSession 的 ExpiresIn 越界應拋 ArgumentOutOfRangeException")]
        public void CreateSession_InvalidExpiresIn_ThrowsArgumentOutOfRange(int expiresIn)
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty);
            var args = new CreateSessionArgs { UserID = "u01", ExpiresIn = expiresIn, OneTime = false };

            Assert.Throws<ArgumentOutOfRangeException>(() => bo.CreateSession(args));
        }

        [Theory]
        [InlineData(DefineType.SystemSettings)]
        [InlineData(DefineType.DatabaseSettings)]
        [DisplayName("GetDefine 非本地呼叫且為敏感 DefineType 應拋 NotSupportedException")]
        public void GetDefine_NonLocalCallWithSensitiveType_ThrowsNotSupported(DefineType defineType)
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty, isLocalCall: false);
            var args = new GetDefineArgs { DefineType = defineType };

            Assert.Throws<NotSupportedException>(() => bo.GetDefine(args));
        }

        [Theory]
        [InlineData(DefineType.SystemSettings)]
        [InlineData(DefineType.DatabaseSettings)]
        [DisplayName("SaveDefine 非本地呼叫且為敏感 DefineType 應拋 NotSupportedException")]
        public void SaveDefine_NonLocalCallWithSensitiveType_ThrowsNotSupported(DefineType defineType)
        {
            var bo = new SystemBusinessObject(TestBeeContext.Create(_fx), Guid.Empty, isLocalCall: false);
            var args = new SaveDefineArgs { DefineType = defineType, Xml = "<root/>" };

            Assert.Throws<NotSupportedException>(() => bo.SaveDefine(args));
        }
    }
}
