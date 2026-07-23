using Bee.Definition.Identity;
using Bee.Definition.Settings;

namespace Bee.Api.Contracts.System
{
    /// <summary>
    /// Contract interface for the EnterCompany response.
    /// </summary>
    public interface IEnterCompanyResponse
    {
        /// <summary>
        /// Gets the company information that was bound to the session.
        /// </summary>
        CompanyInfo Company { get; }

        /// <summary>
        /// Gets the per-model allowed action mask (the capability snapshot) for the session's roles
        /// in the entered company. The client caches this and its element capability resolver reads
        /// it to degrade toolbar commands, grid actions, and sensitive fields. A model absent from
        /// the map means no permission.
        /// </summary>
        Dictionary<string, PermissionAction> Capabilities { get; }
    }
}
