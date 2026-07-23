using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the save definition operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class SaveDefineResponse : ApiResponse, ISaveDefineResponse
    {
    }
}
