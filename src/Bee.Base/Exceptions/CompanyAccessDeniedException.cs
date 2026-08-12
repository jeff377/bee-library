namespace Bee.Base.Exceptions
{
    /// <summary>
    /// Thrown when a caller cannot enter the requested company — because the company does not
    /// exist, is disabled, or the user has not been granted access to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The three causes are deliberately merged into one exception carrying one message, so that
    /// error text cannot be used to enumerate valid company identifiers. The JSON-RPC transport
    /// surfaces this via <c>JsonRpcErrorCode.CompanyAccessDenied</c> (HTTP 403 Forbidden
    /// semantics); the client reconstructs it from that code so callers can
    /// <c>catch (CompanyAccessDeniedException)</c> and route the user back to company selection.
    /// </para>
    /// <para>
    /// Distinct from <see cref="ForbiddenException"/>, which is the per-model+action check inside a
    /// company the caller has already entered.
    /// </para>
    /// </remarks>
    public class CompanyAccessDeniedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyAccessDeniedException"/> class
        /// with the specified message.
        /// </summary>
        /// <param name="message">
        /// The message. Keep it identical for every cause — a message that distinguishes
        /// "no such company" from "not granted" reopens the enumeration channel this type exists
        /// to close.
        /// </param>
        public CompanyAccessDeniedException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyAccessDeniedException"/> class
        /// with the specified message and a reference to the underlying cause.
        /// </summary>
        /// <param name="message">The message; see the single-argument overload on wording.</param>
        /// <param name="innerException">The exception that caused this failure.</param>
        public CompanyAccessDeniedException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
