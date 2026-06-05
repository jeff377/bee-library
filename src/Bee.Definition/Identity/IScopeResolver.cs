using System.Data;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Settings;

namespace Bee.Definition.Identity
{
    /// <summary>
    /// Layer-2 record-scope resolver. Combines the session's record-scope identity
    /// (<c>SessionInfo.UserRowId</c> / <c>EmployeeRowId</c> / <c>DeptRowId</c>), the roles' granted
    /// scope strategies for a (model, action), the permission model's default scope, and the
    /// department tree into a query filter (read side) or a per-row verdict (write side).
    /// </summary>
    /// <remarks>
    /// Multi-role merge: any role granting <c>All</c> → unrestricted (no filter / always in scope);
    /// otherwise the restrictive strategies are OR-unioned. <c>Dept</c> / <c>DeptAndSub</c> implicitly
    /// include <c>Own</c> (a user always sees records they own). A required column or identity that is
    /// missing fails closed (matches no rows).
    /// </remarks>
    public interface IScopeResolver
    {
        /// <summary>
        /// Builds the record-scope filter for a read of (model, action). Returns <c>null</c> when the
        /// scope is unrestricted (the caller applies no extra filter); otherwise a <see cref="FilterNode"/>
        /// to AND with the caller's filter. A fully restricted-but-unsatisfiable scope yields an
        /// always-false node (no rows).
        /// </summary>
        /// <param name="accessToken">The caller's access token (resolves the session snapshot).</param>
        /// <param name="modelId">The permission model id.</param>
        /// <param name="action">The single action being authorized (typically <c>Read</c>).</param>
        /// <param name="formSchema">The form schema (supplies the owner/department columns via <c>ScopeRole</c>).</param>
        FilterNode? ResolveFilter(Guid accessToken, string modelId, PermissionAction action, FormSchema formSchema);

        /// <summary>
        /// Determines whether a single row falls within the effective scope of (model, action) — the
        /// write-side per-row check. Returns <c>true</c> when the scope is unrestricted.
        /// </summary>
        /// <param name="accessToken">The caller's access token (resolves the session snapshot).</param>
        /// <param name="modelId">The permission model id.</param>
        /// <param name="action">The single action implied by the row's state (Create / Update / Delete).</param>
        /// <param name="formSchema">The form schema (supplies the owner/department columns via <c>ScopeRole</c>).</param>
        /// <param name="row">The row being persisted.</param>
        bool IsRowInScope(Guid accessToken, string modelId, PermissionAction action, FormSchema formSchema, DataRow row);
    }
}
