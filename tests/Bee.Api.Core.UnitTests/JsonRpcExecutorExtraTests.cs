using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Api.Core.Messages.System;
using Bee.Definition;
using Bee.Definition.Security;
using Bee.Tests.Shared;

namespace Bee.Api.Core.UnitTests
{
    /// <summary>
    /// JsonRpcExecutor 補強測試：
    /// 涵蓋錯誤路徑（ParseMethod 例外、空 progId、未知 action）、ExecuteAsync 路徑與屬性設定。
    /// </summary>
    // 斷言遮蔽訊息，而遮蔽與否取決於 SysInfo.IsDebugMode 這個 process-wide 靜態，
    // 故與切換該旗標的測試類序列化（見 SysInfoStaticCollection）。
    [Collection("SysInfoStatic")]
    public class JsonRpcExecutorExtraTests : IClassFixture<BeeTestFixture>
    {
        private readonly BeeTestFixture _fx;

        public JsonRpcExecutorExtraTests(BeeTestFixture fx)
        {
            _fx = fx;
        }

        private JsonRpcExecutor NewExecutor(Guid accessToken, bool isLocalCall = false)
        {
            var executor = new JsonRpcExecutor(
                _fx.GetRequiredService<IBusinessObjectFactory>(),
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>())
            {
                AccessToken = accessToken,
                IsLocalCall = isLocalCall,
            };
            return executor;
        }

        [Fact]
        [DisplayName("屬性 setter 應正確設定 AccessToken 與 IsLocalCall")]
        public void Properties_AssignableAfterConstruction()
        {
            var token = Guid.NewGuid();
            var executor = NewExecutor(token, isLocalCall: true);

            Assert.Equal(token, executor.AccessToken);
            Assert.True(executor.IsLocalCall);
        }

        [Fact]
        [DisplayName("IsLocalCall 預設為 false")]
        public void IsLocalCall_DefaultsToFalse()
        {
            var executor = new JsonRpcExecutor(
                _fx.GetRequiredService<IBusinessObjectFactory>(),
                _fx.GetRequiredService<IAccessTokenValidator>(),
                _fx.GetRequiredService<IApiEncryptionKeyProvider>());
            Assert.False(executor.IsLocalCall);
        }

        [Fact]
        [DisplayName("ExecuteAsync 應可正確完成 Ping 方法")]
        public async Task ExecuteAsync_Ping_Succeeds()
        {
            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.Ping",
                Params = new JsonRpcParams { Value = new PingRequest { ClientName = "T", TraceId = "X" } },
                Id = Guid.NewGuid().ToString()
            };

            var response = await NewExecutor(Guid.Empty, isLocalCall: true).ExecuteAsync(request);

            Assert.NotNull(response.Result);
            Assert.Null(response.Error);
            Assert.IsType<PingResponse>(response.Result!.Value);
        }

        [Fact]
        [DisplayName("Execute 於 Method 缺少 '.' 應回傳 FormatException 使用者訊息")]
        public void Execute_MethodMissingDot_ReturnsFormatExceptionMessage()
        {
            var request = new JsonRpcRequest
            {
                Method = "InvalidMethodFormat",
                Params = new JsonRpcParams(),
                Id = "1"
            };

            var response = NewExecutor(Guid.Empty, isLocalCall: true).Execute(request);

            Assert.Null(response.Result);
            Assert.NotNull(response.Error);
            Assert.Equal((int)JsonRpcErrorCode.UserMessage, response.Error!.Code);
            Assert.Contains("Invalid method format", response.Error.Message);
        }

        [Fact]
        [DisplayName("Execute 於 Method 為空字串應回傳 FormatException 使用者訊息")]
        public void Execute_EmptyMethod_ReturnsFormatExceptionMessage()
        {
            var request = new JsonRpcRequest
            {
                Method = string.Empty,
                Params = new JsonRpcParams(),
                Id = "1"
            };

            var response = NewExecutor(Guid.Empty, isLocalCall: true).Execute(request);

            Assert.NotNull(response.Error);
            Assert.Contains("Invalid method format", response.Error!.Message);
        }

        [Fact]
        [DisplayName("Execute 於 progId 為空字串應回傳 ArgumentException 使用者訊息")]
        public void Execute_EmptyProgId_ReturnsArgumentExceptionMessage()
        {
            // ".Ping" 經 ParseMethod 後 progId = ""、action = "Ping"
            var request = new JsonRpcRequest
            {
                Method = ".Ping",
                Params = new JsonRpcParams(),
                Id = "1"
            };

            var response = NewExecutor(Guid.Empty, isLocalCall: true).Execute(request);

            Assert.NotNull(response.Error);
            Assert.Contains("ProgId", response.Error!.Message);
        }

        [Fact]
        [DisplayName("Execute 於未知 Action 應將 MissingMethodException 遮罩為 Internal server error(非 debug 模式)")]
        public void Execute_UnknownAction_ReturnsGenericInternalError()
        {
            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.DefinitelyNotAMethod",
                Params = new JsonRpcParams(),
                Id = "1"
            };

            // 測試 fixture 本身跑在 debug 模式（tests/Define 的 SystemSettings），而遮蔽只在
            // 非 debug 模式生效。這裡要驗的是 production 行為，故明確關掉而不是沿用環境值。
            bool original = SysInfo.IsDebugMode;
            try
            {
                SysInfo.IsDebugMode = false;
                var response = NewExecutor(Guid.Empty, isLocalCall: true).Execute(request);

                Assert.NotNull(response.Error);
                Assert.Equal((int)JsonRpcErrorCode.InternalError, response.Error!.Code);
                Assert.Equal("Internal server error", response.Error.Message);
            }
            finally
            {
                SysInfo.IsDebugMode = original;
            }
        }

        [Fact]
        [DisplayName("Execute 於未知 Action 在 debug 模式應透傳原始例外訊息")]
        public void Execute_UnknownAction_DebugMode_PassesThroughMessage()
        {
            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.DefinitelyNotAMethod",
                Params = new JsonRpcParams(),
                Id = "1"
            };

            bool original = SysInfo.IsDebugMode;
            try
            {
                SysInfo.IsDebugMode = true;
                var response = NewExecutor(Guid.Empty, isLocalCall: true).Execute(request);

                Assert.NotNull(response.Error);
                Assert.Equal((int)JsonRpcErrorCode.InternalError, response.Error!.Code);
                Assert.Contains("DefinitelyNotAMethod", response.Error.Message, StringComparison.Ordinal);
            }
            finally
            {
                SysInfo.IsDebugMode = original;
            }
        }

        [Fact]
        [DisplayName("Execute 於非 System progId 應進入 CreateBusinessObject 分支")]
        public void Execute_NonSystemProgId_InvokesCreateBusinessObject()
        {
            // 使用已定義的 Department progId,未知 action 會被 MissingMethodException 攔截;
            // 無論 Form BO 是否成功建立,CreateBusinessObject 的 else 分支
            // 皆會被執行,覆蓋 CreateBusinessObject 的 delegation。
            var request = new JsonRpcRequest
            {
                Method = "Department.DefinitelyNotAMethod",
                Params = new JsonRpcParams(),
                Id = "1"
            };

            var response = NewExecutor(Guid.Empty, isLocalCall: true).Execute(request);

            Assert.NotNull(response.Error);
        }

        [Fact]
        [DisplayName("Execute 回傳 Response 應回填 Method 與 Id")]
        public void Execute_Response_EchoesMethodAndId()
        {
            var id = Guid.NewGuid().ToString();
            var request = new JsonRpcRequest
            {
                Method = $"{SysProgIds.System}.Ping",
                Params = new JsonRpcParams { Value = new PingRequest { ClientName = "C", TraceId = "T" } },
                Id = id
            };

            var response = NewExecutor(Guid.Empty, isLocalCall: true).Execute(request);

            Assert.Equal(request.Method, response.Method);
            Assert.Equal(id, response.Id);
        }
    }
}
