namespace Bee.Base.Exceptions
{
    /// <summary>
    /// Thrown when an authenticated caller lacks permission for a specific action on a
    /// permission model — the layer-1 model+action authorization check. Distinct from
    /// <see cref="UnauthorizedAccessException"/>, which signals a missing or invalid
    /// credential: here the caller is authenticated but has not been granted the action.
    /// </summary>
    /// <remarks>
    /// The JSON-RPC transport surfaces this via <c>JsonRpcErrorCode.PermissionDenied</c>
    /// (HTTP 403 Forbidden semantics); the client reconstructs it from that code so callers
    /// can <c>catch (ForbiddenException)</c> and degrade the UI accordingly.
    /// </remarks>
    public class ForbiddenException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenException"/> class
        /// with the specified message.
        /// </summary>
        /// <param name="message">The message describing the denied action and model.</param>
        public ForbiddenException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForbiddenException"/> class
        /// with the specified message and a reference to the underlying cause.
        /// </summary>
        /// <param name="message">The message describing the denied action and model.</param>
        /// <param name="innerException">The exception that caused this failure.</param>
        public ForbiddenException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
