using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// Ping 方法的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class PingArgs : BusinessArgs
    {
        /// <summary>
        /// 用戶端識別名稱，可選。
        /// </summary>
        [Key(100)]
        public string ClientName { get; set; }

        /// <summary>
        /// 呼叫追蹤 ID，可選。
        /// </summary>
        [Key(101)]
        public string TraceId { get; set; }
    }

}
