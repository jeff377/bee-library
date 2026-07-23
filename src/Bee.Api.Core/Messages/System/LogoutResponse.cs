using Bee.Api.Contracts.System;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the Logout operation. Carries no payload fields.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class LogoutResponse : ApiResponse, ILogoutResponse
    {
    }
}
