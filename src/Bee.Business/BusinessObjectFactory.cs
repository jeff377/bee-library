using Bee.Business.Form;
using Bee.Business.System;
using Bee.Definition;

namespace Bee.Business
{
    /// <summary>
    /// Default implementation of <see cref="IBusinessObjectFactory"/>; creates business logic objects
    /// (<see cref="SystemBusinessObject"/> or <see cref="FormBusinessObject"/>) for incoming API calls.
    /// </summary>
    public class BusinessObjectFactory : IBusinessObjectFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessObjectFactory"/> class.
        /// </summary>
        public BusinessObjectFactory()
        { }

        /// <summary>
        /// Creates a system-level business logic object.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        public object CreateSystemBusinessObject(Guid accessToken, bool isLocalCall = true)
        {
            return new SystemBusinessObject(accessToken, isLocalCall);
        }

        /// <summary>
        /// Creates a form-level business logic object.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        public object CreateFormBusinessObject(Guid accessToken, string progId, bool isLocalCall = true)
        {
            return new FormBusinessObject(accessToken, progId, isLocalCall);
        }
    }
}
