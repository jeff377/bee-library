using System;
using Bee.Api.Contracts;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for the login operation.
    /// </summary>
    public class LoginResult : BusinessResult, ILoginResponse
    {
        /// <summary>
        /// Gets or sets the access token used for authenticating subsequent API calls.
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the expiration time of the AccessToken in UTC.
        /// </summary>
        public DateTime ExpiredAt { get; set; }

        /// <summary>
        /// Gets or sets the RSA-encrypted session encryption key.
        /// </summary>
        public string ApiEncryptionKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user account identifier.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user display name.
        /// </summary>
        public string UserName { get; set; } = string.Empty;
    }
}
