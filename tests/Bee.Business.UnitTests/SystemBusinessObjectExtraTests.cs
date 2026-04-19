using System.ComponentModel;
using Bee.Base.Serialization;
using Bee.Business.BusinessObjects;
using Bee.Business.System;
using Bee.Business.UnitTests.Fakes;
using Bee.Definition;
using Bee.Definition.Settings;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="SystemBusinessObject"/> 除 Login 外的補強測試：
    /// 涵蓋 Ping / GetCommonConfiguration / GetDefine / SaveDefine 分支、
    /// CheckPackageUpdate / GetPackage 未實作的 NotSupportedException、
    /// 以及 ExecFunc（本地呼叫）基本路徑。
    /// </summary>
    [Collection("Initialize")]
    public class SystemBusinessObjectExtraTests
    {
        [Fact]
        [DisplayName("Ping 應回傳包含 TraceId 與 OK 狀態的 PingResult")]
        public void Ping_ReturnsOkResult()
        {
            var bo = new SystemBusinessObject(Guid.Empty);
            var result = bo.Ping(new PingArgs { TraceId = "T-42", ClientName = "unit" });

            Assert.NotNull(result);
            Assert.Equal("ok", result.Status);
            Assert.Equal("T-42", result.TraceId);
            Assert.True(result.ServerTime <= DateTime.UtcNow);
        }

        [Fact]
        [DisplayName("GetCommonConfiguration 應回傳非空 XML")]
        public void GetCommonConfiguration_ReturnsXml()
        {
            var bo = new SystemBusinessObject(Guid.Empty);
            var result = bo.GetCommonConfiguration(new GetCommonConfigurationArgs());

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.CommonConfiguration));
        }

        [Fact]
        [DisplayName("GetDefine(SystemSettings) 非本地呼叫應拋 NotSupportedException")]
        public void GetDefine_SystemSettings_NonLocal_Throws()
        {
            var bo = new SystemBusinessObject(Guid.Empty, isLocalCall: false);
            Assert.Throws<NotSupportedException>(() =>
                bo.GetDefine(new GetDefineArgs { DefineType = DefineType.SystemSettings }));
        }

        [Fact]
        [DisplayName("GetDefine(DatabaseSettings) 非本地呼叫應拋 NotSupportedException")]
        public void GetDefine_DatabaseSettings_NonLocal_Throws()
        {
            var bo = new SystemBusinessObject(Guid.Empty, isLocalCall: false);
            Assert.Throws<NotSupportedException>(() =>
                bo.GetDefine(new GetDefineArgs { DefineType = DefineType.DatabaseSettings }));
        }

        [Fact]
        [DisplayName("GetDefine(FormSchema) 本地呼叫應回傳含 XML 的結果")]
        public void GetDefine_FormSchema_ReturnsXml()
        {
            var bo = new SystemBusinessObject(Guid.Empty, isLocalCall: true);
            var result = bo.GetDefine(new GetDefineArgs
            {
                DefineType = DefineType.FormSchema,
                Keys = new[] { "Department" }
            });

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.Xml));
        }

        [Fact]
        [DisplayName("SaveDefine(SystemSettings) 非本地呼叫應拋 NotSupportedException")]
        public void SaveDefine_SystemSettings_NonLocal_Throws()
        {
            var bo = new SystemBusinessObject(Guid.Empty, isLocalCall: false);
            Assert.Throws<NotSupportedException>(() =>
                bo.SaveDefine(new SaveDefineArgs { DefineType = DefineType.SystemSettings, Xml = "<x/>" }));
        }

        [Fact]
        [DisplayName("SaveDefine(DatabaseSettings) 非本地呼叫應拋 NotSupportedException")]
        public void SaveDefine_DatabaseSettings_NonLocal_Throws()
        {
            var bo = new SystemBusinessObject(Guid.Empty, isLocalCall: false);
            Assert.Throws<NotSupportedException>(() =>
                bo.SaveDefine(new SaveDefineArgs { DefineType = DefineType.DatabaseSettings, Xml = "<x/>" }));
        }

        [Fact]
        [DisplayName("CheckPackageUpdate 基底實作應拋 NotSupportedException")]
        public void CheckPackageUpdate_BaseImpl_Throws()
        {
            var bo = new SystemBusinessObject(Guid.Empty);
            Assert.Throws<NotSupportedException>(() =>
                bo.CheckPackageUpdate(new CheckPackageUpdateArgs()));
        }

        [Fact]
        [DisplayName("GetPackage 基底實作應拋 NotSupportedException")]
        public void GetPackage_BaseImpl_Throws()
        {
            var bo = new SystemBusinessObject(Guid.Empty);
            Assert.Throws<NotSupportedException>(() =>
                bo.GetPackage(new GetPackageArgs()));
        }

        [Fact]
        [DisplayName("ExecFuncAnonymous(Hello) 應回傳 Hello 問候")]
        public void ExecFuncAnonymous_Hello_ReturnsGreeting()
        {
            // SystemExecFuncHandler.Hello 標註 ApiAccessRequirement.Anonymous，透過 DoExecFuncAnonymous 呼叫
            var bo = new TestableSystemBusinessObject(Guid.Empty, _ => (false, string.Empty));
            var args = new ExecFuncArgs("Hello");

            var result = bo.ExecFuncAnonymous(args);

            Assert.True(result.Parameters.Contains("Hello"));
            Assert.Contains("Hello system-level", result.Parameters.GetValue<string>("Hello"));
        }
    }
}
