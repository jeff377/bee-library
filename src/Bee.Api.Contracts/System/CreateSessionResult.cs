using System;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Output result for creating a user session.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class CreateSessionResult : BusinessResult
    {
        /// <summary>
        /// Gets or sets the access token used for authenticating subsequent API calls.
        /// </summary>
        [Key(100)]
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the expiration time of the AccessToken in UTC.
        /// The client should complete all protected API calls before this time.
        /// </summary>
        [Key(101)]
        public DateTime ExpiredAt { get; set; }
    }
}
