using System.ComponentModel;
using System.Reflection;
using Bee.Api.Client.Connectors;
using Bee.Api.Client.Providers;
using Bee.Api.Core.JsonRpc;
using Bee.Api.Core.Messages;
using Bee.Api.Core.Messages.AuditLog;
using Bee.Definition;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// <see cref="LogApiConnector"/> 九個方法的路由測試。
    /// </summary>
    /// <remarks>
    /// 這九個方法都是薄包裝，真正的風險是**路由抄錯**：其中五個共用
    /// <c>LogListResponse</c>、三個共用 <c>LogAggregateResponse</c>，因此把
    /// <c>GetDbAnomalyLog</c> 寫成 <c>GetApiAnomalyLog</c> 會回傳看起來完全合理的資料，
    /// 型別系統與 round-trip 測試都不會出聲。這裡逐一釘住送出的
    /// <c>Method</c>（<c>progId.action</c>）與「原封不動傳遞 request 物件」。
    /// </remarks>
    public class LogApiConnectorRoutingTests
    {
        private sealed class CapturingProvider : IJsonRpcProvider
        {
            public JsonRpcRequest? LastRequest { get; private set; }
            public object? ResultValue { get; set; }

            public Task<JsonRpcResponse> ExecuteAsync(JsonRpcRequest request)
            {
                LastRequest = request;
                var result = new JsonRpcResult { Value = ResultValue };

                // 連線器預設要求 Encrypted，但未登入時（無傳輸金鑰）會降級為 Encoded；
                // 這裡照同一格式回覆，整條 restore 路徑才走得完。
                ApiPayloadConverter.TransformTo(result, PayloadFormat.Encoded);
                return Task.FromResult(new JsonRpcResponse(request) { Result = result });
            }
        }

        private static (LogApiConnector Connector, CapturingProvider Provider) Create(object resultValue)
        {
            var connector = new LogApiConnector(Guid.NewGuid());
            var provider = new CapturingProvider { ResultValue = resultValue };
            typeof(ApiConnector)
                .GetProperty(nameof(ApiConnector.Provider), BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(connector, provider);
            return (connector, provider);
        }

        /// <summary>
        /// 每個方法一列：呼叫方式、預期的 action、以及該方法的回應型別實例。
        /// </summary>
        public static TheoryData<string, string> RoutedActions => new()
        {
            { nameof(LogApiConnector.GetChangeLogAsync),          LogActions.GetChangeLog },
            { nameof(LogApiConnector.GetChangeDetailAsync),       LogActions.GetChangeDetail },
            { nameof(LogApiConnector.GetLoginLogAsync),           LogActions.GetLoginLog },
            { nameof(LogApiConnector.GetAccessLogAsync),          LogActions.GetAccessLog },
            { nameof(LogApiConnector.GetApiAnomalyLogAsync),      LogActions.GetApiAnomalyLog },
            { nameof(LogApiConnector.GetDbAnomalyLogAsync),       LogActions.GetDbAnomalyLog },
            { nameof(LogApiConnector.GetApiAnomalySummaryAsync),  LogActions.GetApiAnomalySummary },
            { nameof(LogApiConnector.GetDbAnomalySummaryAsync),   LogActions.GetDbAnomalySummary },
            { nameof(LogApiConnector.GetTopApiMethodsAsync),      LogActions.GetTopApiMethods },
        };

        [Theory]
        [MemberData(nameof(RoutedActions))]
        [DisplayName("每個方法都必須送出自己那一個 action，且 progId 為 AuditLog")]
        public async Task Method_RoutesToItsOwnAction(string methodName, string expectedAction)
        {
            var (provider, expectedRequestType) = await InvokeAsync(methodName);

            Assert.NotNull(provider.LastRequest);
            Assert.Equal($"{SysProgIds.AuditLog}.{expectedAction}", provider.LastRequest!.Method);

            // 同時釘住送出的 request 型別：只比對 action 字串的話，「action 對、卻塞錯 request
            // 型別」這種抄錯仍會通過（多個方法共用同一個回應型別，看不出差異）。
            Assert.StartsWith(expectedRequestType.FullName!, provider.LastRequest.Params.TypeName, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("九個 action 必須兩兩不同（抄錯路由的直接徵兆）")]
        public void RoutedActions_AreAllDistinct()
        {
            var actions = RoutedActions.Select(row => (string)row[1]).ToList();

            Assert.Equal(actions.Count, actions.Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        [DisplayName("GetChangeDetailAsync 應把 sysRowId 包進 request 的對應欄位")]
        public async Task GetChangeDetailAsync_WrapsSysRowId()
        {
            var (connector, provider) = Create(new GetChangeDetailResponse());
            var sysRowId = Guid.NewGuid();

            await connector.GetChangeDetailAsync(sysRowId);

            // payload 在送出前已轉為 Encoded（Value 被序列化進 bytes），故解回來檢查——
            // 這也順帶證明 request 真的完整上了 wire，而不是只有型別名對。
            var payload = provider.LastRequest!.Params;
            ApiPayloadConverter.RestoreFrom(payload, PayloadFormat.Encoded);
            var sent = Assert.IsType<GetChangeDetailRequest>(payload.Value);
            Assert.Equal(sysRowId, sent.SysRowId);
        }

        /// <summary>
        /// 依方法名叫用，並回傳對應的 provider。回應值用該方法宣告的回傳型別的新實例。
        /// </summary>
        private static async Task<(CapturingProvider Provider, Type RequestType)> InvokeAsync(string methodName)
        {
            var method = typeof(LogApiConnector).GetMethod(methodName)
                ?? throw new InvalidOperationException($"找不到 {methodName}。");
            var responseType = method.ReturnType.GetGenericArguments()[0];
            var (connector, provider) = Create(Activator.CreateInstance(responseType)!);

            var parameter = method.GetParameters()[0];
            var isGuid = parameter.ParameterType == typeof(Guid);
            var argument = isGuid
                ? (object)Guid.NewGuid()
                : Activator.CreateInstance(parameter.ParameterType)!;

            await (Task)method.Invoke(connector, [argument])!;

            // 取 Guid 的那個多載自己包 request，預期型別由方法名推導。
            var requestType = isGuid ? typeof(GetChangeDetailRequest) : parameter.ParameterType;
            return (provider, requestType);
        }
    }
}
