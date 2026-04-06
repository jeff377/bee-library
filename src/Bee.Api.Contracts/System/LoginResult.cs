using System;
using MessagePack;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Output result for the login operation.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class LoginResult : BusinessResult
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

        /// <summary>
        /// Gets or sets the RSA-encrypted session encryption key.
        /// </summary>
        [Key(102)]
        public string ApiEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user account identifier.
        /// </summary>
        [Key(103)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user display name.
        /// </summary>
        [Key(104)]
        public string UserName { get; set; } = string.Empty;
    }
}
