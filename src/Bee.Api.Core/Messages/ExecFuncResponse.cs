using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages
{
    /// <summary>
    /// API response type for executing a custom method.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class ExecFuncResponse : ApiResponse, IExecFuncResponse
    {
    }
}
