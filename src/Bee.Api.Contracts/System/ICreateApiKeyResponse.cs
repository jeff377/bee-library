namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the create API key response.
    /// </summary>
    public interface ICreateApiKeyResponse
    {
        /// <summary>
        /// Gets the key identifier that was issued.
        /// </summary>
        string SysId { get; }

        /// <summary>
        /// Gets the complete plaintext key, returned exactly once.
        /// </summary>
        /// <remarks>
        /// IMPORTANT: only the hash is stored, so the framework cannot reproduce this value. A caller
        /// that loses it has to issue a replacement key and retire this one — which is the rotation
        /// procedure anyway. Any interface showing this value must say so; presenting it like a
        /// password field that can be revealed again would be a lie.
        /// </remarks>
        string ApiKey { get; }
    }
}
