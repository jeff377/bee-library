using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the create session operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class CreateSessionResponse : ApiResponse, ICreateSessionResponse
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the expiration time of the AccessToken in UTC.
        /// </summary>
        public DateTime ExpiredAt { get; set; }
    }
}
