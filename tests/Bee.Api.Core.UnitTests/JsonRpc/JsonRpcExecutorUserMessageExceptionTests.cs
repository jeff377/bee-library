using System.ComponentModel;
using Bee.Api.Core.JsonRpc;
using Bee.Base;
using Bee.Base.Exceptions;

namespace Bee.Api.Core.UnitTests.JsonRpc
{
    /// <summary>
    /// Tests for <see cref="JsonRpcExecutor.MapException"/> covering the mapping from
    /// exception types to (<see cref="JsonRpcErrorCode"/>, message) pairs used in the
    /// JSON-RPC response envelope.
    /// </summary>
    [Collection("SysInfoStatic")]
    public class JsonRpcExecutorUserMessageExceptionTests
    {
        [Fact]
        [DisplayName("MapException 於 UserMessageException 應回傳 UserMessage code 與原訊息")]
        public void MapException_UserMessageException_ReturnsUserMessageCode()
        {
            var ex = new UserMessageException("欄位不能為空");

            var (code, message) = JsonRpcExecutor.MapException(ex);

            Assert.Equal(JsonRpcErrorCode.UserMessage, code);
            Assert.Equal("欄位不能為空", message);
        }

        [Fact]
        [DisplayName("MapException 於 ForbiddenException 應回傳 PermissionDenied code 與原訊息")]
        public void MapException_ForbiddenException_ReturnsPermissionDeniedCode()
        {
            var ex = new ForbiddenException("Permission denied: 'Delete' on model 'PurchaseOrder'.");

            var (code, message) = JsonRpcExecutor.MapException(ex);

            Assert.Equal(JsonRpcErrorCode.PermissionDenied, code);
            Assert.Equal("Permission denied: 'Delete' on model 'PurchaseOrder'.", message);
        }

        [Fact]
        [DisplayName("MapException 於白名單 BCL 例外應回傳 UserMessage code(過渡期相容)")]
        public void MapException_BclWhitelistException_ReturnsUserMessageCode()
        {
            var ex = new InvalidOperationException("Company access denied.");

            var (code, message) = JsonRpcExecutor.MapException(ex);

            Assert.Equal(JsonRpcErrorCode.UserMessage, code);
            Assert.Equal("Company access denied.", message);
        }

        [Fact]
        [DisplayName("MapException 於 ArgumentException 應回傳 UserMessage code")]
        public void MapException_ArgumentException_ReturnsUserMessageCode()
        {
            var ex = new ArgumentException("CompanyId is required.");

            var (code, _) = JsonRpcExecutor.MapException(ex);

            Assert.Equal(JsonRpcErrorCode.UserMessage, code);
        }

        [Fact]
        [DisplayName("MapException 於 UnauthorizedAccessException 應回傳 UserMessage code")]
        public void MapException_UnauthorizedAccessException_ReturnsUserMessageCode()
        {
            var ex = new UnauthorizedAccessException("Session not found or has expired.");

            var (code, _) = JsonRpcExecutor.MapException(ex);

            Assert.Equal(JsonRpcErrorCode.UserMessage, code);
        }

        [Fact]
        [DisplayName("MapException 於 FormatException 應回傳 UserMessage code")]
        public void MapException_FormatException_ReturnsUserMessageCode()
        {
            var ex = new FormatException("Invalid method format.");

            var (code, _) = JsonRpcExecutor.MapException(ex);

            Assert.Equal(JsonRpcErrorCode.UserMessage, code);
        }

        [Fact]
        [DisplayName("MapException 於非白名單例外應回傳 InternalError code 與遮蔽訊息(非 debug 模式)")]
        public void MapException_UnknownException_ReturnsInternalErrorCode()
        {
            var ex = new MissingMethodException("Method 'DefinitelyNotAMethod' not found.");

            // 遮蔽只在非 debug 模式生效，而測試行程本身跑在 debug 模式
            // （tests/Define 的 SystemSettings）。這裡要驗的是 production 行為。
            bool original = SysInfo.IsDebugMode;
            try
            {
                SysInfo.IsDebugMode = false;
                var (code, message) = JsonRpcExecutor.MapException(ex);

                Assert.Equal(JsonRpcErrorCode.InternalError, code);
                Assert.Equal("Internal server error", message);
            }
            finally
            {
                SysInfo.IsDebugMode = original;
            }
        }

        [Fact]
        [DisplayName("MapException 於通用 Exception 應回傳 InternalError code(非 debug 模式不洩漏原訊息)")]
        public void MapException_GenericException_ReturnsInternalErrorCode()
        {
            var ex = new Exception("Some internal failure.");

            bool original = SysInfo.IsDebugMode;
            try
            {
                SysInfo.IsDebugMode = false;
                var (code, message) = JsonRpcExecutor.MapException(ex);

                Assert.Equal(JsonRpcErrorCode.InternalError, code);
                Assert.Equal("Internal server error", message);
                // 確認原訊息不洩漏。
                Assert.DoesNotContain("Some internal failure", message);
            }
            finally
            {
                SysInfo.IsDebugMode = original;
            }
        }

        [Fact]
        [DisplayName("MapException 於 debug 模式應透傳基礎設施例外原訊息(code 維持 InternalError)")]
        public void MapException_DebugMode_PassesThroughInfrastructureMessage()
        {
            bool original = SysInfo.IsDebugMode;
            try
            {
                SysInfo.IsDebugMode = true;
                var ex = new MissingMethodException("Method 'DefinitelyNotAMethod' not found.");

                var (code, message) = JsonRpcExecutor.MapException(ex);

                Assert.Equal(JsonRpcErrorCode.InternalError, code);
                Assert.Equal("Method 'DefinitelyNotAMethod' not found.", message);
            }
            finally
            {
                SysInfo.IsDebugMode = original;
            }
        }

        [Fact]
        [DisplayName("MapException 於 debug 模式不應改變使用者面例外的對應結果")]
        public void MapException_DebugMode_LeavesUserFacingMappingUnchanged()
        {
            bool original = SysInfo.IsDebugMode;
            try
            {
                SysInfo.IsDebugMode = true;
                var ex = new UserMessageException("欄位不能為空");

                var (code, message) = JsonRpcExecutor.MapException(ex);

                Assert.Equal(JsonRpcErrorCode.UserMessage, code);
                Assert.Equal("欄位不能為空", message);
            }
            finally
            {
                SysInfo.IsDebugMode = original;
            }
        }
    }
}
