namespace Bee.Base.Exceptions
{
    /// <summary>
    /// Thrown when an operation needs a company context but the session has none — either
    /// <c>EnterCompany</c> was never called, or <c>LeaveCompany</c> has cleared it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The condition is detected at a single choke point rather than per business method: any
    /// repository access whose scope is company-level resolves its database through
    /// <c>IRepositoryDatabaseRouter</c>, and that resolution is impossible without a company.
    /// </para>
    /// <para>
    /// The JSON-RPC transport surfaces this via <c>CompanyNotEntered</c>
    /// (HTTP 409 Conflict semantics); the client reconstructs it from that code so callers can
    /// <c>catch (CompanyNotEnteredException)</c> and send the user to company selection.
    /// This is a recoverable protocol state, not a business message: it must not be shown to the
    /// user verbatim.
    /// </para>
    /// </remarks>
    public class CompanyNotEnteredException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyNotEnteredException"/> class
        /// with the specified message.
        /// </summary>
        /// <param name="message">The message describing the missing company context.</param>
        public CompanyNotEnteredException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyNotEnteredException"/> class
        /// with the specified message and a reference to the underlying cause.
        /// </summary>
        /// <param name="message">The message describing the missing company context.</param>
        /// <param name="innerException">The exception that caused this failure.</param>
        public CompanyNotEnteredException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
