using Bee.Api.Contracts.System;
using Bee.Definition.Identity;
using Bee.Definition.Settings;

namespace Bee.Business.System
{
    /// <summary>
    /// Output result for the EnterCompany operation.
    /// </summary>
    public class EnterCompanyResult : BusinessResult, IEnterCompanyResponse
    {
        /// <summary>
        /// Gets or sets the company information that was bound to the session.
        /// </summary>
        public CompanyInfo Company { get; set; } = new CompanyInfo();

        /// <summary>
        /// Gets or sets the per-model allowed action mask (capability snapshot) for the session's
        /// roles in the entered company. Copied to the wire response by <c>ApiOutputConverter</c>.
        /// </summary>
        public Dictionary<string, PermissionAction> Capabilities { get; set; } = [];
    }
}
