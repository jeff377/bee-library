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
        public ISystemBusinessObject CreateSystemBusinessObject(Guid accessToken)
        {
            return new SystemBusinessObject(accessToken);
        }

        /// <summary>
        /// Creates a form-level business logic object.
        /// </summary>
        /// <param name="accessToken">Access token.</param>
        /// <param name="progId">Program ID.</param>
        public IFormBusinessObject CreateFormBusinessObject(Guid accessToken, string progId)
        {
            switch (progId)
            {
                case "Employee": 
                    return new EmployeeBusinessObject(accessToken, progId);
                default:
                    return new FormBusinessObject(accessToken, progId);
            }
        }
    }
}
