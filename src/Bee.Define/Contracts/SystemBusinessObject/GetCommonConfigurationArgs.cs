using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 取得通用參數及環境設置的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetCommonConfigurationArgs : BusinessArgs
    {
    }
}
