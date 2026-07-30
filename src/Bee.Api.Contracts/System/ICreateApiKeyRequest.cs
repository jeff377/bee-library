using Bee.Definition.Security;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the create API key request.
    /// </summary>
    public interface ICreateApiKeyRequest
    {
        /// <summary>
        /// Gets the key identifier to issue, which becomes the leading segment of the plaintext key.
        /// Lowercase letters, digits and hyphens only.
        /// </summary>
        string SysId { get; }

        /// <summary>
        /// Gets the display name of the application this key is for.
        /// </summary>
        string SysName { get; }

        /// <summary>
        /// Gets the key classification. A label for operators; it carries no authorization meaning.
        /// </summary>
        ApiKeyType KeyType { get; }

        /// <summary>
        /// Gets the contact for the third party holding this key, so an incident has someone to reach.
        /// </summary>
        string? Contact { get; }

        /// <summary>
        /// Gets the UTC expiry, or <c>null</c> for a key that does not expire.
        /// </summary>
        DateTime? ExpiredAt { get; }
    }
}
