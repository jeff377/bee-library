using System;

namespace Bee.Define
{
    /// <summary>
    /// Interface for a business object provider that defines how all business objects are obtained.
    /// </summary>
    public interface IBusinessObjectProvider
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
