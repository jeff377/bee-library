using Bee.Definition.Security;

namespace Bee.LoadTests.Configuration
{
    /// <summary>
    /// Which backend to drive and how to talk to it.
    /// </summary>
    public sealed class TargetOptions
    {
        /// <summary>
        /// Gets or sets whether to dispatch in-process or over HTTP.
        /// </summary>
        public TargetMode Mode { get; set; } = TargetMode.Local;

        /// <summary>
        /// Gets or sets the endpoint to POST to. Required when <see cref="Mode"/> is
        /// <see cref="TargetMode.Remote"/>, ignored otherwise.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the protection level the payloads are sent at. Encryption is the
        /// expensive one, so a run at <see cref="ApiProtectionLevel.Public"/> and one at
        /// <see cref="ApiProtectionLevel.Encrypted"/> together show what crypto costs.
        /// </summary>
        public ApiProtectionLevel ProtectionLevel { get; set; } = ApiProtectionLevel.Encrypted;

        /// <summary>
        /// Gets or sets the body codec name. Empty means the client does not declare one, which
        /// the server reads as MessagePack.
        /// </summary>
        public string Codec { get; set; } = string.Empty;
    }
}
