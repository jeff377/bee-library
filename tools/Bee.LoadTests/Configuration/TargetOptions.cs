using Bee.Definition.Security;

namespace Bee.LoadTests.Configuration
{
    /// <summary>
    /// Which backend to drive and how to talk to it.
    /// </summary>
    public sealed class TargetOptions
    {
        /// <summary>
        /// The port <see cref="ResolveServeUrl"/> listens on when <see cref="ServeUrl"/> is empty.
        /// </summary>
        public const int DefaultServePort = 5199;

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

        /// <summary>
        /// Gets or sets the address the <c>serve</c> command listens on. Empty takes the loopback
        /// default; <c>--url</c> overrides both.
        /// </summary>
        /// <remarks>
        /// A Remote run needs the address here and the one in <see cref="Endpoint"/> to agree, and
        /// the two ends are often on different machines, so the listen address belongs in the same
        /// configuration file as the rest of the run rather than in the driver.
        /// </remarks>
        public string ServeUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value sent in the <c>X-Api-Key</c> header on remote calls.
        /// </summary>
        /// <remarks>
        /// A deployment with no enabled API key checks the header for presence only, so any
        /// non-empty value gets a run moving; the default is a label rather than a secret. Point a
        /// run at a deployment that does have keys enabled and this has to carry a real one, which
        /// is why it is configuration rather than a constant.
        /// </remarks>
        public string ApiKey { get; set; } = "loadtest";

        /// <summary>
        /// Gets the address the <c>serve</c> command should listen on.
        /// </summary>
        /// <returns>
        /// <see cref="ServeUrl"/> when it is set, otherwise loopback on
        /// <see cref="DefaultServePort"/>.
        /// </returns>
        public string ResolveServeUrl()
            => string.IsNullOrWhiteSpace(ServeUrl)
                ? FormattableString.Invariant($"http://localhost:{DefaultServePort}")
                : ServeUrl;
    }
}
