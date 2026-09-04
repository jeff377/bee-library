
using Bee.Api.Core.Messages;

namespace Bee.Api.Core.Validator
{
    /// <summary>
    /// Represents the context of the current API call, describing its state.
    /// </summary>
    /// <remarks>
    /// Lives beside <see cref="ApiAccessValidator"/> rather than under <c>Messages</c>, because it
    /// is the validator's input and never crosses the wire — <see cref="Bee.Api.Core.JsonRpc.JsonRpcExecutor"/> builds
    /// one per call and hands it straight to the validator.
    /// <para>
    /// WARNING: it used to sit in the <c>Messages</c> namespace, and that had two consequences,
    /// neither of them intended. The TypeScript contract generator publishes every public class in
    /// that namespace, so this type — which carries <see cref="IsLocalCall"/>, the flag several
    /// second lines of defence key on — was published as a shape a client could name and
    /// instantiate. It also dragged <see cref="Bee.Api.Core.Messages.PayloadFormat"/> into the generated contract as a string
    /// union (<c>'Plain' | 'Encoded' | 'Encrypted'</c>) while the value actually on the wire is a
    /// number, so the contract disagreed with the wire on a field of the same name.
    /// </para>
    /// </remarks>
    public class ApiCallContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiCallContext"/> class.
        /// </summary>
        public ApiCallContext()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiCallContext"/> class.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="isLocalCall">Indicates whether the call originates from a local source.</param>
        /// <param name="format">The payload encoding format for transmission.</param>
        public ApiCallContext(Guid accessToken, bool isLocalCall, PayloadFormat format)
        {
            AccessToken = accessToken;
            IsLocalCall = isLocalCall;
            Format = format;
        }

        /// <summary>
        /// Gets or sets the access token used to identify the current user or session.
        /// </summary>
        public Guid AccessToken { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the call originates from a local source (e.g., the same process or host as the server).
        /// </summary>
        public bool IsLocalCall { get; set; }

        /// <summary>
        /// Gets or sets the payload format of the call.
        /// </summary>
        public PayloadFormat Format { get; set; }
    }
}
