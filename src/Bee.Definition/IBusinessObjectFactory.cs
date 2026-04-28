namespace Bee.Definition
{
    /// <summary>
    /// Factory for creating business objects. Used by the API layer to create the BO instance
    /// that handles a particular API call (system-level or form-level).
    /// </summary>
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
    }
}
