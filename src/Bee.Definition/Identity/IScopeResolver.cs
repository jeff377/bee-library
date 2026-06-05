using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Settings;

namespace Bee.Definition.Identity
{
    /// <summary>
    /// Layer-2 record-scope resolver. Combines the session's record-scope identity
    /// (<c>SessionInfo.UserRowId</c> / <c>EmployeeRowId</c> / <c>DeptRowId</c>), the roles' granted
    /// scope strategies for a (model, action), the permission model's default scope, and the
    /// department tree into a query filter.
    /// </summary>
    /// <remarks>
    /// The same filter drives both the read path (AND it into list / single-row queries) and the
    /// write path (re-query the target row by id AND this filter; absent → out of scope). Enforcing
    /// writes by an authoritative re-query — rather than evaluating client-supplied row values —
    /// keeps the security boundary on the server (a forged payload cannot relabel its way in).
    /// <para>
    /// Multi-role merge: any role granting <c>All</c> → unrestricted (no filter); otherwise the
    /// restrictive strategies are OR-unioned. <c>Dept</c> / <c>DeptAndSub</c> implicitly include
    /// <c>Own</c> (a user always sees records they own). A required column or identity that is
    /// missing fails closed (matches no rows).
    /// </para>
    /// </remarks>
    public interface IScopeResolver
    {
        /// <summary>
        /// Builds the record-scope filter for (model, action). Returns <c>null</c> when the scope is
        /// unrestricted (the caller applies no extra filter); otherwise a <see cref="FilterNode"/> to
        /// AND with the caller's filter (read) or with the row-id predicate (write re-query). A fully
        /// restricted-but-unsatisfiable scope yields an always-false node (no rows).
        /// </summary>
        /// <param name="accessToken">The caller's access token (resolves the session snapshot).</param>
        /// <param name="modelId">The permission model id.</param>
        /// <param name="action">The single action being authorized (<c>Read</c> / <c>Update</c> / <c>Delete</c>).</param>
        /// <param name="formSchema">The form schema (supplies the owner/department columns via <c>ScopeRole</c>).</param>
        FilterNode? ResolveFilter(Guid accessToken, string modelId, PermissionAction action, FormSchema formSchema);
    }
}
