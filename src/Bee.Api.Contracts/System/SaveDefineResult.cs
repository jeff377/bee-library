using System;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// 儲存定義資料的傳出結果。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class SaveDefineResult : BusinessResult
    {
    }
}
