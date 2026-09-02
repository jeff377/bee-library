using System.ComponentModel;
using Bee.Api.Client.Connectors;
using Bee.Api.Core.JsonRpc;
using Bee.Base.Exceptions;

namespace Bee.Api.Client.UnitTests.Connectors
{
    /// <summary>
    /// Tests for the response-finalization branch of <see cref="ApiConnector"/>,
    /// covering the mapping from <see cref="JsonRpcError.Code"/> back to client-side
    /// exception types (round-trip with <c>JsonRpcExecutor.MapException</c>).
    /// </summary>
    /// <remarks>
    /// 本檔逐則驗證單一錯誤碼的行為與訊息形狀；「兩端對映是否一致、有無新碼漏接」
    /// 由 <see cref="ErrorContractDriftTests"/> 守。
    /// </remarks>
    public class ApiConnectorFinalizeResponseTests
    {
        [Fact]
        [DisplayName("FinalizeResponse 於 UserMessage code 應拋出 UserMessageException 且訊息純淨無前綴")]
        public async Task FinalizeResponse_UserMessageCode_ThrowsUserMessageException()
        {
            var ex = await Assert.ThrowsAsync<UserMessageException>(() =>
                ApiConnectorTestHost.ExecuteWithErrorAsync(JsonRpcErrorCode.UserMessage, "欄位不能為空"));

            Assert.Equal("欄位不能為空", ex.Message);
            Assert.DoesNotContain("API error", ex.Message);
        }

        [Fact]
        [DisplayName("FinalizeResponse 於 PermissionDenied code 應拋出 ForbiddenException 且訊息純淨無前綴")]
        public async Task FinalizeResponse_PermissionDeniedCode_ThrowsForbiddenException()
        {
            const string message = "Permission denied: 'Delete' on model 'PurchaseOrder'.";

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                ApiConnectorTestHost.ExecuteWithErrorAsync(JsonRpcErrorCode.PermissionDenied, message));

            Assert.Equal(message, ex.Message);
            Assert.DoesNotContain("API error", ex.Message);
        }

        [Fact]
        [DisplayName("FinalizeResponse 於 ReplayRejected code 應拋出 ReplayRejectedException 且訊息純淨無前綴")]
        public async Task FinalizeResponse_ReplayRejectedCode_ThrowsReplayRejectedException()
        {
            const string message = "The request timestamp is 90 seconds away from server time, outside the accepted window.";

            var ex = await Assert.ThrowsAsync<ReplayRejectedException>(() =>
                ApiConnectorTestHost.ExecuteWithErrorAsync(JsonRpcErrorCode.ReplayRejected, message));

            Assert.Equal(message, ex.Message);
            Assert.DoesNotContain("API error", ex.Message);
        }

        [Fact]
        [DisplayName("FinalizeResponse 於 InternalError code 應拋出 InvalidOperationException 並保留前綴格式")]
        public async Task FinalizeResponse_InternalErrorCode_ThrowsInvalidOperationException()
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ApiConnectorTestHost.ExecuteWithErrorAsync(JsonRpcErrorCode.InternalError, "Internal server error"));

            Assert.Contains("API error", ex.Message);
            Assert.Contains("-32000", ex.Message);
            Assert.Contains("Internal server error", ex.Message);
        }

        [Fact]
        [DisplayName("FinalizeResponse 於 ParseError 等其他協定 code 應拋出 InvalidOperationException(迴歸)")]
        public async Task FinalizeResponse_OtherProtocolCode_ThrowsInvalidOperationException()
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                ApiConnectorTestHost.ExecuteWithErrorAsync(JsonRpcErrorCode.MethodNotFound, "Method not found"));

            Assert.Contains("-32601", ex.Message);
            Assert.Contains("Method not found", ex.Message);
        }

        [Fact]
        [DisplayName("UserMessageException 可被 catch (Exception) 接住(迴歸:既有寬泛 catch 仍能運作)")]
        public async Task FinalizeResponse_UserMessageException_StillCaughtAsException()
        {
            Exception? caught = null;
            try
            {
                await ApiConnectorTestHost.ExecuteWithErrorAsync(JsonRpcErrorCode.UserMessage, "test");
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            Assert.NotNull(caught);
            Assert.IsType<UserMessageException>(caught);
        }

        [Fact]
        [DisplayName("FinalizeResponse 於成功響應應正常回傳結果(迴歸)")]
        public async Task FinalizeResponse_NoError_ReturnsValue()
        {
            var result = await ApiConnectorTestHost.ExecuteWithResultAsync();

            Assert.Equal("ok", result);
        }
    }
}
