namespace Bee.Definition
{
    /// <summary>
    /// Factory for creating business objects. Used by the API layer to create the BO instance that
    /// handles a particular API call, selected by progId through the type registry.
    /// </summary>
    /// <remarks>
    /// Return type is <c>object</c> rather than <c>IBusinessObject</c> to avoid a
    /// reverse dependency from <c>Bee.Definition</c> to <c>Bee.Business</c> (where
    /// <c>IBusinessObject</c> lives). Callers cast to <c>IBusinessObject</c> at use sites.
    /// </remarks>
    public interface IBusinessObjectFactory
    {
        /// <summary>
        /// Creates the business object registered for the supplied progId.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program ID.</param>
        /// <param name="isLocalCall">
        /// Indicates whether the call originates from a local source. There is deliberately no
        /// default: a local call bypasses <c>ApiAccessValidator</c> entirely, so the caller must
        /// state which side of that boundary it is on rather than inherit the permissive value.
        /// </param>
        object CreateBusinessObject(Guid accessToken, string progId, bool isLocalCall);
    }
}
