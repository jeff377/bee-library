using Bee.Api.Contracts.Form;
using MessagePack;

namespace Bee.Api.Core.Messages.Form
{
    /// <summary>
    /// API request for the form GetNewData operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class GetNewDataRequest : ApiRequest, IGetNewDataRequest
    {
    }
}
