using Bee.Business.Form;
using Bee.Business.System;
using Bee.Definition;

namespace Bee.Business
{
    /// <summary>
    /// Provider for creating business logic objects.
    /// </summary>
    public class BusinessObjectProvider : IBusinessObjectProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessObjectProvider"/> class.
        /// </summary>
        public BusinessObjectProvider()
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
