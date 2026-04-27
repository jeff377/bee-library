using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
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
