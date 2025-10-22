using System;
using Bee.Business;
using Bee.Define;

namespace Custom.Business
{
    /// <summary>
    /// Provider of business logic objects.
    /// </summary>
    public class BusinessObjectProvider : IBusinessObjectProvider
    {
        /// <summary>
        /// Constructor。
        /// </summary>
        public BusinessObjectProvider()
        { }

        /// <summary>
        /// Creates a system-level business logic object.
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        /// <param name="isLocalCall">Indicates whether the call is from a local source.</param>
        public object CreateSystemBusinessObject(Guid accessToken, bool isLocalCall = true)
        {
            return new SystemBusinessObject(accessToken, isLocalCall);
        }

        /// <summary>
        /// Creates a form-level business logic object.
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        /// <param name="progId">Program ID.</param>
        /// <param name="isLocalCall">Indicates whether the call is from a local source.</param>
        public object CreateFormBusinessObject(Guid accessToken, string progId, bool isLocalCall = true)
        {
            switch (progId)
            {
                case "Employee": 
                    return new EmployeeBusinessObject(accessToken, progId, isLocalCall);
                default:
                    return new FormBusinessObject(accessToken, progId, isLocalCall);
            }
        }
    }
}
