using Bee.Api.Contracts;
using MessagePack;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.System
{
    /// <summary>
    /// API response for the save definition operation.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class SaveDefineResponse : ApiResponse, ISaveDefineResponse
    {
    }
}
