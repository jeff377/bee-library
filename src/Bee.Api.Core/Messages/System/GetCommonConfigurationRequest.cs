using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the get common configuration operation.
    /// </summary>
    [MessagePackObject]
    public class GetCommonConfigurationRequest : ApiRequest, IGetCommonConfigurationRequest
    {
    }
}
