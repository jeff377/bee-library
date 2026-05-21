namespace Bee.Base.Exceptions
{
    /// <summary>
    /// Represents a user-facing message produced by business logic, intended to be
    /// shown to the end user (for example: validation failure, business-rule
    /// violation, or workflow interruption).
    /// </summary>
    /// <remarks>
    /// Conceptually this is a "business flow interruption signal" rather than a
    /// genuine program error: control flow is aborted because the operation cannot
    /// be completed, and the message is meant to reach the user as-is. The C# layer
    /// still throws (matching .NET conventions for flow control), and the JSON-RPC
    /// transport layer surfaces it via <c>JsonRpcErrorCode.UserMessage</c>.
    ///
    /// <para>
    /// Prefer this type over BCL exceptions (<see cref="InvalidOperationException"/>,
    /// <see cref="ArgumentException"/>, etc.) for any message that is meant to be
    /// surfaced to end users. BCL exceptions remain available during the migration
    /// period but will be phased out of the user-facing whitelist in future plans.
    /// </para>
    /// </remarks>
    public class UserMessageException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserMessageException"/> class
        /// with the specified user-facing message.
        /// </summary>
        /// <param name="message">The message to display to the end user.</param>
        public UserMessageException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMessageException"/> class
        /// with the specified user-facing message and a reference to the underlying
        /// cause.
        /// </summary>
        /// <param name="message">The message to display to the end user.</param>
        /// <param name="innerException">The exception that caused this failure.</param>
        public UserMessageException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
