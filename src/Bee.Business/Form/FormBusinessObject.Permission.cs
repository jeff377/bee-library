using System.Data;
using Bee.Base;
using Bee.Base.Exceptions;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Identity;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.Form;

namespace Bee.Business.Form
{
    /// <summary>
    /// Permission-gate half of FormBusinessObject; split out for file size only.
    /// </summary>
    public partial class FormBusinessObject
    {
        #region 權限驗證（層一 model+action gate）

        // 寫入動作的判定順序；層一不涉 record scope，故每個 action 只需判一次（逐列等價）。
        private static readonly PermissionAction[] s_writeActions =
            { PermissionAction.Create, PermissionAction.Update, PermissionAction.Delete };

        /// <summary>
        /// Enforces the layer-1 permission check for <paramref name="action"/> on this form's
        /// permission model. A no-op when the FormSchema declares no <c>PermissionModelId</c>
        /// (gradual adoption — unmarked forms stay open). Throws when the caller lacks the grant.
        /// </summary>
        /// <param name="action">The single <see cref="PermissionAction"/> flag to require.</param>
        /// <exception cref="ForbiddenException">The caller is not granted the action.</exception>
        private void Authorize(PermissionAction action)
        {
            var modelId = DefineAccess.GetFormSchema(ProgId).PermissionModelId;
            if (string.IsNullOrEmpty(modelId)) { return; }

            var authorization = Services.GetRequiredService<IAuthorizationService>();
            if (!authorization.Can(AccessToken, modelId, action))
                throw new ForbiddenException($"Permission denied: '{action}' on model '{modelId}'.");
        }

        /// <summary>
        /// Resolves the layer-2 record-scope read filter for <paramref name="action"/> on this form's
        /// permission model. Returns <c>null</c> when the FormSchema declares no <c>PermissionModelId</c>
        /// (unscoped form) or the effective scope is unrestricted — in both cases no filter is applied.
        /// </summary>
        /// <param name="action">The action whose read scope is resolved (typically <c>Read</c>).</param>
        private FilterNode? ResolveScopeFilter(PermissionAction action)
        {
            var schema = DefineAccess.GetFormSchema(ProgId);
            if (string.IsNullOrEmpty(schema.PermissionModelId)) { return null; }

            var resolver = Services.GetRequiredService<IScopeResolver>();
            return resolver.ResolveFilter(AccessToken, schema.PermissionModelId, action, schema);
        }

        /// <summary>
        /// AND-combines the caller-supplied list filter with the record-scope filter (either may be <c>null</c>).
        /// </summary>
        private static FilterNode? CombineWithScope(FilterNode? clientFilter, FilterNode? scopeFilter)
        {
            if (scopeFilter == null) { return clientFilter; }
            if (clientFilter == null) { return scopeFilter; }
            return FilterGroup.All(scopeFilter, clientFilter);
        }

        /// <summary>
        /// Whether the DataSet saves an existing master record — i.e. the master table has a row whose
        /// state is anything other than <c>Added</c> (Modified / Unchanged / Deleted). These are the
        /// saves layer-2 record scope re-checks; a pure insert (only <c>Added</c> master rows) is not.
        /// </summary>
        /// <param name="dataSet">The DataSet about to be persisted.</param>
        private bool HasExistingMasterWrite(DataSet dataSet)
        {
            var masterTableName = DefineAccess.GetFormSchema(ProgId).MasterTable?.TableName;
            if (string.IsNullOrEmpty(masterTableName) || !dataSet.Tables.Contains(masterTableName)) { return false; }

            foreach (DataRow row in dataSet.Tables[masterTableName]!.Rows)
            {
                if (row.RowState != DataRowState.Added) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Enforces layer-2 record scope on writes by authoritatively re-querying each saved master
        /// row. <c>Deleted</c> → Delete scope; <c>Modified</c> / <c>Unchanged</c> → Update scope (a
        /// details-only edit leaves the master Unchanged but still updates the record). Each is
        /// confirmed in the caller's scope against the database — not the supplied payload, so a
        /// forged DataSet cannot relabel its way past the boundary. <c>Added</c> (Create) rows are not
        /// scope-checked (a new row has no existing scope to violate; creation is governed by the
        /// action grant). A no-op when the form declares no <c>PermissionModelId</c> or the action's
        /// scope is unrestricted.
        /// </summary>
        /// <param name="dataSet">The DataSet about to be persisted.</param>
        /// <param name="repository">The repository used for the authoritative in-scope check.</param>
        /// <exception cref="ForbiddenException">A mutated master row is outside the caller's scope.</exception>
        private void EnforceWriteScope(DataSet dataSet, IDataFormRepository repository)
        {
            var schema = DefineAccess.GetFormSchema(ProgId);
            if (string.IsNullOrEmpty(schema.PermissionModelId)) { return; }

            var masterTableName = schema.MasterTable?.TableName;
            if (string.IsNullOrEmpty(masterTableName) || !dataSet.Tables.Contains(masterTableName)) { return; }

            // Resolve the scope filter only once an Update/Delete row is found, and at most once per
            // action — an insert-only save resolves nothing; N same-action rows reuse one filter.
            IScopeResolver? resolver = null;
            var scopeByAction = new Dictionary<PermissionAction, FilterNode?>();

            foreach (DataRow row in dataSet.Tables[masterTableName]!.Rows)
            {
                var action = WriteScopeActionForRowState(row.RowState);
                if (action == PermissionAction.None) { continue; }

                if (!scopeByAction.TryGetValue(action, out var scopeFilter))
                {
                    resolver ??= Services.GetRequiredService<IScopeResolver>();
                    scopeFilter = resolver.ResolveFilter(AccessToken, schema.PermissionModelId, action, schema);
                    scopeByAction[action] = scopeFilter;
                }
                if (scopeFilter == null) { continue; }

                var version = row.RowState == DataRowState.Deleted ? DataRowVersion.Original : DataRowVersion.Default;
                var rowId = ValueUtilities.CGuid(row[SysFields.RowId, version]);
                if (!repository.ExistsInScope(rowId, scopeFilter))
                    throw new ForbiddenException($"Record out of scope for '{action}' on model '{schema.PermissionModelId}'.");
            }
        }

        /// <summary>
        /// Enforces the layer-1 permission check for a Save by deriving the required actions
        /// from each row's <c>RowState</c> (Added→Create / Modified→Update / Deleted→Delete)
        /// and verifying every distinct action present in the DataSet.
        /// </summary>
        /// <param name="dataSet">The DataSet about to be persisted.</param>
        /// <exception cref="ForbiddenException">The caller lacks one of the required actions.</exception>
        private void AuthorizeSave(DataSet dataSet)
        {
            var modelId = DefineAccess.GetFormSchema(ProgId).PermissionModelId;
            if (string.IsNullOrEmpty(modelId)) { return; }

            var required = CollectRowStateActions(dataSet);
            if (required == PermissionAction.None) { return; }

            var authorization = Services.GetRequiredService<IAuthorizationService>();

            // s_writeActions holds only non-zero flags, so None is a safe "no denial" sentinel.
            var denied = s_writeActions.FirstOrDefault(
                action => required.HasFlag(action) && !authorization.Can(AccessToken, modelId, action));
            if (denied != PermissionAction.None)
                throw new ForbiddenException($"Permission denied: '{denied}' on model '{modelId}'.");
        }

        /// <summary>
        /// Maps a master row's <c>RowState</c> to the <see cref="PermissionAction"/> whose record
        /// scope must be enforced on write. <c>Added</c> (Create) returns <see cref="PermissionAction.None"/>
        /// because a new row has no existing scope to violate; <c>Modified</c> and <c>Unchanged</c>
        /// both map to <see cref="PermissionAction.Update"/> (a details-only edit leaves the master
        /// Unchanged but still persists the record).
        /// </summary>
        private static PermissionAction WriteScopeActionForRowState(DataRowState state) => state switch
        {
            DataRowState.Added => PermissionAction.None,
            DataRowState.Deleted => PermissionAction.Delete,
            _ => PermissionAction.Update,
        };

        /// <summary>
        /// OR-merges the <see cref="PermissionAction"/> implied by every row's <c>RowState</c>
        /// across all tables in the DataSet.
        /// </summary>
        private static PermissionAction CollectRowStateActions(DataSet dataSet)
        {
            var actions = PermissionAction.None;
            foreach (DataTable table in dataSet.Tables)
            {
                foreach (DataRow row in table.Rows)
                {
                    actions |= row.RowState switch
                    {
                        DataRowState.Added => PermissionAction.Create,
                        DataRowState.Modified => PermissionAction.Update,
                        DataRowState.Deleted => PermissionAction.Delete,
                        _ => PermissionAction.None,
                    };
                }
            }
            return actions;
        }

        #endregion
    }
}
