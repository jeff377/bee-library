namespace Bee.Api.Core
{
    /// <summary>
    /// 呼叫上下文，描述目前 API 呼叫的狀態。
    /// </summary>
    public class TApiCallContext
    {
        /// <summary>
        /// 呼叫是否為近端來源（例如與伺服器同一進程或主機）。
        /// </summary>
        public bool IsLocalCall { get; set; }

        /// <summary>
        /// 呼叫是否為經過編碼的傳輸（例如加密與壓縮）。
        /// </summary>
        public bool IsEncoded { get; set; }
    }
}
