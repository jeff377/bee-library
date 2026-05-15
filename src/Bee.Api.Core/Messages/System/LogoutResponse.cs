using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the Logout operation. Carries no payload fields.
    /// </summary>
    [MessagePackObject]
    public class LogoutResponse : ApiResponse, ILogoutResponse
    {
    }
}
