namespace Bee.Api.Contracts
{
    /// <summary>
    /// Contract interface for login response data.
    /// </summary>
    public interface ILoginResponse
    {
        /// <summary>
        /// Gets the access token used for authenticating subsequent API calls.
        /// </summary>
        Guid AccessToken { get; }

        /// <summary>
        /// Gets the expiration time of the AccessToken in UTC.
        /// </summary>
        DateTime ExpiredAt { get; }

        /// <summary>
        /// Gets the RSA-encrypted session encryption key.
        /// </summary>
        string ApiEncryptionKey { get; }

        /// <summary>
        /// Gets the user account identifier.
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Gets the user display name.
        /// </summary>
        string UserName { get; }
    }
}
