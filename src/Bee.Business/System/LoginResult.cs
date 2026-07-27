using Bee.Api.Contracts.System;

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

        /// <summary>
        /// Gets or sets the user's IANA time zone id (e.g. Asia/Taipei).
        /// </summary>
        /// <remarks>
        /// Returned so the client can render dates and seed new rows on the user's own day rather
        /// than the device's — a user filing a Taipei leave request from New York must still default
        /// to the Taipei date (ADR-032 D12).
        /// </remarks>
        public string TimeZone { get; set; } = string.Empty;

    }
}
