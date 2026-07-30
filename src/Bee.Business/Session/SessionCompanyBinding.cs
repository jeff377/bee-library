using Bee.Definition.Identity;
using Bee.Definition.Settings;

namespace Bee.Business.Session
{
    /// <summary>
    /// The outcome of binding a session to a company: the company record and the capability
    /// snapshot computed for the session's roles within it.
    /// </summary>
    /// <param name="Company">The company the session was bound to.</param>
    /// <param name="Capabilities">
    /// The per-model allowed action mask for the session's roles in that company.
    /// </param>
    public sealed record SessionCompanyBinding(
        CompanyInfo Company,
        Dictionary<string, PermissionAction> Capabilities);
}
