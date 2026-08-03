using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the list API keys operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class ListApiKeysRequest : ApiRequest, IListApiKeysRequest
    {
    }
}
