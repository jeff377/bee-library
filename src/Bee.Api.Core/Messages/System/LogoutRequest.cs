using Bee.Api.Contracts.System;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the Logout operation. Carries no payload fields.
    /// </summary>
    public class LogoutRequest : ApiRequest, ILogoutRequest
    {
    }
}
