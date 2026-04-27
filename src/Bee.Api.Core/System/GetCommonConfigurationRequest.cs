using Bee.Api.Contracts;
using MessagePack;
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.System
{
    /// <summary>
    /// API request for the get common configuration operation.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class GetCommonConfigurationRequest : ApiRequest, IGetCommonConfigurationRequest
    {
    }
}
