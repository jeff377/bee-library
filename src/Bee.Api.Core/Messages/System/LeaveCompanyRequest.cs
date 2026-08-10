using Bee.Api.Contracts.System;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the LeaveCompany operation. Carries no payload fields.
    /// </summary>
    public class LeaveCompanyRequest : ApiRequest, ILeaveCompanyRequest
    {
    }
}
