namespace Bee.Definition
{
    /// <summary>
    /// Factory for creating business objects. Used by the API layer to create the BO instance
    /// that handles a particular API call (system-level or form-level).
    /// </summary>
    /// <remarks>
    /// Return type is <see cref="object"/> rather than <c>IBusinessObject</c> to avoid a
    /// reverse dependency from <c>Bee.Definition</c> to <c>Bee.Business</c> (where
    /// <c>IBusinessObject</c> lives). Callers cast to <c>IBusinessObject</c> at use sites.
    /// </remarks>
    public interface IBusinessObjectFactory
    {
        /// <summary>
        /// Creates a system-level business object.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="isLocalCall">Indicates whether the call originates from a local source.</param>
        object CreateSystemBusinessObject(Guid accessToken, bool isLocalCall = true);

        /// <summary>
        /// Creates a form-level business object.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program ID.</param>
        /// <param name="isLocalCall">Indicates whether the call originates from a local source.</param>
        object CreateFormBusinessObject(Guid accessToken, string progId, bool isLocalCall = true);

        /// <summary>
        /// Creates the audit-log business object (read-only queries over the <c>st_log_*</c> tables).
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="isLocalCall">Indicates whether the call originates from a local source.</param>
        object CreateLogBusinessObject(Guid accessToken, bool isLocalCall = true);
    }
}
