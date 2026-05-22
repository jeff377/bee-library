using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API request for the form GetNewData operation.
    /// </summary>
    [MessagePackObject]
    public class GetNewDataRequest : ApiRequest, IGetNewDataRequest
    {
    }
}
