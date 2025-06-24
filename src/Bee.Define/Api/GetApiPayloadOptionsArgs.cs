using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 取得 API 傳輸層的 Payload 編碼選項的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetApiPayloadOptionsArgs : BusinessArgs
    {
    }
}
