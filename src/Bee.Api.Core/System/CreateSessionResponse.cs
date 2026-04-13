using System;
using Bee.Api.Contracts;
using MessagePack;

namespace Bee.Api.Core.System
{
    /// <summary>
    /// API response for the create session operation.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class CreateSessionResponse : ApiResponse, ICreateSessionResponse
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        [Key(100)]
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the expiration time of the AccessToken in UTC.
        /// </summary>
        [Key(101)]
        public DateTime ExpiredAt { get; set; }
    }
}
