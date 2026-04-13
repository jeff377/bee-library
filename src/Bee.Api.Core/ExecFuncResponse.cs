using System;
using MessagePack;

namespace Bee.Api.Core
{
    /// <summary>
    /// API response type for executing a custom method.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class ExecFuncResponse : ApiResponse
    {
    }
}
