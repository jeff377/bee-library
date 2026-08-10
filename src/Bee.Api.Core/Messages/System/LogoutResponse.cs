using Bee.Api.Contracts.System;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the Logout operation. Carries no payload fields.
    /// </summary>
    public class LogoutResponse : ApiResponse, ILogoutResponse
    {
    }
}
