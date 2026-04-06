using System;
using MessagePack;

namespace Bee.Api.Contracts
{
    /// <summary>
    /// Output result for executing a custom method.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class ExecFuncResult : BusinessResult
    {
    }
}
