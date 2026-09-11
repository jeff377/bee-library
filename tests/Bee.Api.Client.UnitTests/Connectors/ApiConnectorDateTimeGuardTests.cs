using System.ComponentModel;
using Bee.Api.Core.Messages.Form;
using Bee.Definition.Filters;

namespace Bee.Api.Client.UnitTests.Connectors
{
    /// <summary>
    /// 驗證 <see cref="Bee.Api.Client.Connectors.ApiConnector"/> 送出請求前的
    /// <see cref="Bee.Api.Core.JsonRpc.DateTimeWireGuard"/> 檢查不因使用者時區換算而失效（ADR-032 D6）。
    /// </summary>
    /// <remarks>
    /// 時區換算會把過濾條件值改為 <see cref="DateTimeKind.Unspecified"/>，guard 若排在換算之後，
    /// 登入後（有使用者時區）的 <see cref="DateTimeKind.Local"/> 值就一律放行。
    /// 只測 guard 本身的 <c>DateTimeWireGuardTests</c> 看不到這個先後順序。
    /// </remarks>
    public class ApiConnectorDateTimeGuardTests
    {
        private static readonly DateTime s_sample = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Unspecified);

        [Theory]
        [InlineData("")]
        [InlineData("Asia/Taipei")]
        [DisplayName("過濾條件帶 Kind=Local 時，不論是否設定使用者時區都應擲例外")]
        public async Task ExecuteAsync_FilterWithLocalKind_ThrowsRegardlessOfUserTimeZone(string userTimeZoneId)
        {
            var request = Request(DateTime.SpecifyKind(s_sample, DateTimeKind.Local));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => ApiConnectorTestHost.ExecuteAsUserAsync(request, userTimeZoneId));

            Assert.Contains("created_at", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("已設定使用者時區時，Kind=Unspecified 的過濾條件值應正常送出")]
        public async Task ExecuteAsync_FilterWithUnspecifiedKindAndUserTimeZone_Succeeds()
        {
            var result = await ApiConnectorTestHost.ExecuteAsUserAsync(Request(s_sample), "Asia/Taipei");

            Assert.Equal("ok", result);
        }

        private static GetListRequest Request(DateTime value)
            => new GetListRequest { Filter = FilterCondition.Equal("created_at", value) };
    }
}
