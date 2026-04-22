using System.ComponentModel;
using Bee.Api.Client.Connectors;
using Bee.Api.Core;
using Bee.Definition;
using Bee.Definition.Settings;

namespace Bee.Api.Client.UnitTests
{
    [Collection("Initialize")]
    public class SystemApiConnectorLocalTests
    {
        [Fact]
        [DisplayName("ExecFuncAsync 本機連線呼叫 Hello 應成功回應")]
        public async Task ExecFuncAsync_LocalConnector_Hello_Succeeds()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var args = new ExecFuncRequest("Hello");
            var exception = await Record.ExceptionAsync(() => connector.ExecFuncAsync(args));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("ExecFunc 同步本機連線呼叫 Hello 應成功回應")]
        public void ExecFunc_LocalConnector_Hello_Succeeds()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var args = new ExecFuncRequest("Hello");
            var exception = Record.Exception(() => connector.ExecFunc(args));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("ExecFuncAnonymousAsync 本機連線呼叫 Hello 應成功回應")]
        public async Task ExecFuncAnonymousAsync_LocalConnector_Hello_Succeeds()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var args = new ExecFuncRequest("Hello");
            var exception = await Record.ExceptionAsync(() => connector.ExecFuncAnonymousAsync(args));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("ExecFuncLocalAsync 本機連線呼叫 Hello 應成功回應")]
        public async Task ExecFuncLocalAsync_LocalConnector_Hello_Succeeds()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var args = new ExecFuncRequest("Hello");
            var exception = await Record.ExceptionAsync(() => connector.ExecFuncLocalAsync(args));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("InitializeAsync 本機連線應成功執行系統初始化")]
        public async Task InitializeAsync_LocalConnector_Succeeds()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var exception = await Record.ExceptionAsync(() => connector.InitializeAsync());
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("Initialize 同步本機連線應成功執行系統初始化")]
        public void Initialize_LocalConnector_Succeeds()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var exception = Record.Exception(() => connector.Initialize());
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("GetDefineAsync 本機連線取得 SystemSettings 應回傳非 null 結果")]
        public async Task GetDefineAsync_LocalConnector_SystemSettings_ReturnsResult()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var result = await connector.GetDefineAsync<SystemSettings>(DefineType.SystemSettings);
            Assert.NotNull(result);
        }

        [Fact]
        [DisplayName("GetDefine 同步本機連線取得 SystemSettings 應回傳非 null 結果")]
        public void GetDefine_LocalConnector_SystemSettings_ReturnsResult()
        {
            var connector = new SystemApiConnector(Guid.NewGuid());
            var result = connector.GetDefine<SystemSettings>(DefineType.SystemSettings);
            Assert.NotNull(result);
        }
    }
}
