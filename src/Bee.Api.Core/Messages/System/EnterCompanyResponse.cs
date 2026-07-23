using Bee.Api.Contracts;
using Bee.Definition.Identity;
using Bee.Definition.Settings;
using MessagePack;

namespace Bee.Api.Core.Messages.System
{
    /// <summary>
    /// API response for the EnterCompany operation.
    /// </summary>
    [MessagePackObject(keyAsPropertyName: true)]
    public class EnterCompanyResponse : ApiResponse, IEnterCompanyResponse
    {
        /// <summary>
        /// Gets or sets the company information that was bound to the session.
        /// </summary>
        public CompanyInfo Company { get; set; } = new CompanyInfo();

        /// <summary>
        /// Gets or sets the per-model allowed action mask (capability snapshot) for the session's
        /// roles in the entered company. The client caches this on <c>ClientInfo</c> and its element
        /// capability resolver reads it to degrade toolbar commands, grid actions, and sensitive
        /// fields. A model absent from the map means no permission.
        /// </summary>
        public Dictionary<string, PermissionAction> Capabilities { get; set; } = [];
    }
}
