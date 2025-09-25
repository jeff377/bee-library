using System;

namespace Bee.Connect
{
    /// <summary>
    ///  服務連線方式。
    /// </summary>
    public enum ConnectType
    {
        /// <summary>
        /// 近端連線。
        /// </summary>
        Local,
        /// <summary>
        /// 遠端連線。
        /// </summary>
        Remote
    }

    /// <summary>
    /// 程式支援的服務連線方式。
    /// </summary>
    [Flags]
    public enum SupportedConnectTypes
    {
        /// <summary>
        /// 近端連線。
        /// </summary>
        Local = 1,
        /// <summary>
        /// 遠端連線。
        /// </summary>
        Remote = 2,
        /// <summary>
        /// 同時支援近端及遠端連線。
        /// </summary>
        Both = Local | Remote
    }
}
