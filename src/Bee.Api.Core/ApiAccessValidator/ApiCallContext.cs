using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// 呼叫上下文，描述目前 API 呼叫的狀態。
    /// </summary>
    public class ApiCallContext
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        public ApiCallContext()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="accessToken">存取令牌。</param>
        /// <param name="isLocalCall">呼叫是否為近端來源。</param>
        /// <param name="format">傳輸資料的封裝格式。</param>
        public ApiCallContext(Guid accessToken, bool isLocalCall, PayloadFormat format)
        {
            AccessToken = accessToken;
            IsLocalCall = isLocalCall;
            Format = format;
        }

        /// <summary>
        /// 存取令牌，用於識別目前使用者或工作階段。
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// 呼叫是否為近端來源（例如與伺服器同一進程或主機）。
        /// </summary>
        public bool IsLocalCall { get; set; }

        /// <summary>
        /// 呼叫的有效負載格式。
        /// </summary>
        public PayloadFormat Format { get; set; }

        /// <summary>
        /// 是否應該驗證編碼（只有遠端呼叫才需驗證）。
        /// </summary>
        public bool ShouldValidateEncoding => !IsLocalCall;
    }
}
