using System;

namespace Bee.Define
{
    /// <summary>
    /// Ping 方法的傳入引數。
    /// </summary>
    [Serializable]
    public class TPingArgs : TBusinessArgs
    {
        /// <summary>
        /// 用戶端識別名稱，可選。
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// 呼叫追蹤 ID，可選。
        /// </summary>
        public string TraceId { get; set; }
    }

}
