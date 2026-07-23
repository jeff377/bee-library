using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API request for the create session operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class CreateSessionRequest : ApiRequest, ICreateSessionRequest
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public string UserID { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the session expiration time in seconds.
        /// </summary>
        public int ExpiresIn { get; set; } = 3600;

        /// <summary>
        /// Gets or sets a value indicating whether this is a one-time session.
        /// </summary>
        public bool OneTime { get; set; } = false;
    }
}
