using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the LeaveCompany operation. Carries no payload fields.
    /// </summary>
    [MessagePackObject]
    public class LeaveCompanyRequest : ApiRequest, ILeaveCompanyRequest
    {
    }
}
