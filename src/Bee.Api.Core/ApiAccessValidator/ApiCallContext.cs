using System;

namespace Bee.Api.Core
{
    /// <summary>
    /// Represents the context of the current API call, describing its state.
    /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether encoding should be validated (only required for remote calls).
        /// </summary>
        public bool ShouldValidateEncoding => !IsLocalCall;
    }
}
