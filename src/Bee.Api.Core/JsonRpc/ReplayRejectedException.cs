namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Thrown when a call is refused by the replay-protection gate: the wire frame is missing,
    /// unreadable, or its timestamp falls outside the accepted window.
    /// </summary>
    /// <remarks>
    /// This is a distinct type rather than a reuse of <see cref="UnauthorizedAccessException"/> so
    /// that the caller can tell "retrying will not help" apart from "the credential was rejected".
    /// It maps to <see cref="JsonRpcErrorCode.ReplayRejected"/>.
    /// </remarks>
    public sealed class ReplayRejectedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayRejectedException"/> class.
        /// </summary>
        /// <param name="message">The message describing why the call was refused.</param>
        public ReplayRejectedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayRejectedException"/> class.
        /// </summary>
        /// <param name="message">The message describing why the call was refused.</param>
        /// <param name="innerException">The underlying cause.</param>
        public ReplayRejectedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
