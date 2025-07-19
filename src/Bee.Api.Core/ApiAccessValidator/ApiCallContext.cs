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
        /// <param name="isLocalCall">呼叫是否為近端來源。</param>
        /// <param name="format">傳輸資料的封裝格式。</param>
        public ApiCallContext(bool isLocalCall, PayloadFormat format)
        {
            IsLocalCall = isLocalCall;
            Format = format;
        }

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
