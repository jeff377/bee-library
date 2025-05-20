using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 取得 API 傳輸層的編碼設定的傳入引數。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class TGetEncodingProfileArgs : TBusinessArgs
    {
    }
}
