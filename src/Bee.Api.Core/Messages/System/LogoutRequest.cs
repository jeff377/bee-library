using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the Logout operation. Carries no payload fields.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class LogoutRequest : ApiRequest, ILogoutRequest
    {
    }
}
