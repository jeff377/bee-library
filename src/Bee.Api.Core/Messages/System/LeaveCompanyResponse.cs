using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the LeaveCompany operation. Carries no payload fields.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class LeaveCompanyResponse : ApiResponse, ILeaveCompanyResponse
    {
    }
}
