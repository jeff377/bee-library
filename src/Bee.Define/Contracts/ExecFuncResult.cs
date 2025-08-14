using System;
using MessagePack;

namespace Bee.Define
{
    /// <summary>
    /// 執行自訂方法的傳出結果。
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class ExecFuncResult : BusinessResult
    {
    }
}
