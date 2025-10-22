using System;
using MessagePack;

namespace Bee.Contracts
{
    /// <summary>
    /// 取得通用參數及環境設置的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetCommonConfigurationArgs : BusinessArgs
    {
    }

    /// <summary>
    /// 取得通用參數及環境設置的傳出結果。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetCommonConfigurationResult : BusinessResult
    {
        /// <summary>
        /// 通用參數及環境設置。
        /// </summary>
        [Key(100)]
        public string CommonConfiguration { get; set; } = string.Empty;
    }
}
