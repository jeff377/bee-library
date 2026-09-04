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
        #region Authorization (layer-1 model + action gate)

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

            var authorization = Services.GetRequiredService<ICompanyAuthorizationService>();
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
        /// Enforces layer-2 record scope on writes by authoritatively re-querying each saved master
        /// row. <c>Deleted</c> → Delete scope; <c>Modified</c> / <c>Unchanged</c> → Update scope (a
        /// details-only edit leaves the master Unchanged but still updates the record). Each is
        /// confirmed in the caller's scope against the database — not the supplied payload, so a
        /// forged DataSet cannot relabel its way past the boundary. <c>Added</c> (Create) rows are not
        /// scope-checked (a new row has no existing scope to violate; creation is governed by the
        /// action grant). A no-op when the form declares no <c>PermissionModelId</c> or the action's
        /// scope is unrestricted.
        /// </summary>
        /// <remarks>
        /// <para>
        /// WARNING: scope is master-only — the record persists with the master that passed. That only
        /// holds while <b>every</b> written detail row belongs to a master row in this payload, so two
        /// structural checks guard it, and both are load-bearing:
        /// </para>
        /// <list type="number">
        /// <item><description>A payload that carries detail rows but <b>no master table at all</b> is
        ///   refused. Without it the whole method returned early — no master table meant nothing to
        ///   scope-check — while the repository went on writing the details.</description></item>
        /// <item><description>Every written detail row's <see cref="SysFields.MasterRowId"/> must name
        ///   a master row present here. Without it a payload could pair one in-scope master with
        ///   details pointing at someone else's, and re-parent existing detail rows on the way — the
        ///   full column set is written on update, so that column is not read-only in practice.</description></item>
        /// </list>
        /// <para>
        /// Both are refusals rather than silent drops: the shapes have no legitimate use. The framework's
        /// own details-only edit carries the master row in <c>Unchanged</c> state, which is exactly why
        /// <see cref="WriteScopeActionForRowState"/> maps that state to Update.
        /// </para>
        /// </remarks>
        /// <param name="dataSet">The DataSet about to be persisted.</param>
        /// <param name="repository">The repository used for the authoritative in-scope check.</param>
        /// <exception cref="ForbiddenException">A mutated master row is outside the caller's scope, or a
        /// detail row does not belong to a master row in this payload.</exception>
        private void EnforceWriteScope(DataSet dataSet, IDataFormRepository repository)
        {
            var schema = DefineAccess.GetFormSchema(ProgId);
            if (string.IsNullOrEmpty(schema.PermissionModelId)) { return; }

            var masterTableName = schema.MasterTable?.TableName;
            if (string.IsNullOrEmpty(masterTableName)) { return; }

            if (!dataSet.Tables.Contains(masterTableName))
            {
                if (HasPendingRows(dataSet))
                {
                    throw new ForbiddenException(
                        $"Save must carry the '{masterTableName}' row the details belong to; " +
                        $"record scope on model '{schema.PermissionModelId}' cannot be resolved without it.");
                }
                return;
            }

            var masterTable = dataSet.Tables[masterTableName]!;
            bool hasRowId = masterTable.Columns.Contains(SysFields.RowId);
            var savedMasterRowIds = new HashSet<Guid>();

            // Resolve the scope filter only once an Update/Delete row is found, and at most once per
            // action — an insert-only save resolves nothing; N same-action rows reuse one filter.
            IScopeResolver? resolver = null;
            var scopeByAction = new Dictionary<PermissionAction, FilterNode?>();

            foreach (DataRow row in masterTable.Rows)
            {
                var version = row.RowState == DataRowState.Deleted ? DataRowVersion.Original : DataRowVersion.Default;
                var rowId = hasRowId ? ValueUtilities.CGuid(row[SysFields.RowId, version]) : Guid.Empty;

                // Collected for every state, Added included: the details of a brand-new master
                // reference the rowid this payload is inserting.
                if (rowId != Guid.Empty) { savedMasterRowIds.Add(rowId); }

                var action = WriteScopeActionForRowState(row.RowState);
                if (action == PermissionAction.None) { continue; }

                if (!scopeByAction.TryGetValue(action, out var scopeFilter))
                {
                    resolver ??= Services.GetRequiredService<IScopeResolver>();
                    scopeFilter = resolver.ResolveFilter(AccessToken, schema.PermissionModelId, action, schema);
                    scopeByAction[action] = scopeFilter;
                }
                if (scopeFilter == null) { continue; }

                if (!repository.ExistsInScope(rowId, scopeFilter))
                    throw new ForbiddenException($"Record out of scope for '{action}' on model '{schema.PermissionModelId}'.");
            }

            EnforceDetailOwnership(dataSet, masterTableName, savedMasterRowIds, schema.PermissionModelId);
        }

        /// <summary>
        /// Whether any table in the DataSet has a row the repository would write.
        /// </summary>
        /// <param name="dataSet">The DataSet about to be persisted.</param>
        private static bool HasPendingRows(DataSet dataSet)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                foreach (DataRow row in table.Rows)
                {
                    if (row.RowState != DataRowState.Unchanged) { return true; }
                }
            }
            return false;
        }

        /// <summary>
        /// Requires every written detail row to belong to a master row carried by this payload —
        /// those are the rows <see cref="EnforceWriteScope"/> has just confirmed in the caller's scope.
        /// </summary>
        /// <remarks>
        /// <c>Unchanged</c> rows are skipped because the repository never writes them. A
        /// <c>Modified</c> row is checked on both versions: the current value is where it is moving to,
        /// the original is where it is moving from, and taking a row out of someone else's record is
        /// as much a scope violation as putting one into it. Detail tables that declare no
        /// <see cref="SysFields.MasterRowId"/> are skipped — the column the repository would write
        /// does not exist, so there is nothing to forge.
        /// </remarks>
        /// <param name="dataSet">The DataSet about to be persisted.</param>
        /// <param name="masterTableName">The master table's name.</param>
        /// <param name="savedMasterRowIds">The master rowids carried by this payload.</param>
        /// <param name="modelId">The permission model, for the failure message.</param>
        /// <exception cref="ForbiddenException">A detail row names a master outside this payload.</exception>
        private static void EnforceDetailOwnership(
            DataSet dataSet, string masterTableName, HashSet<Guid> savedMasterRowIds, string modelId)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                if (StringUtilities.IsEquals(table.TableName, masterTableName)) { continue; }
                if (!table.Columns.Contains(SysFields.MasterRowId)) { continue; }

                foreach (DataRow row in table.Rows)
                {
                    foreach (var version in WrittenVersions(row.RowState))
                    {
                        var owner = ValueUtilities.CGuid(row[SysFields.MasterRowId, version]);
                        if (owner == Guid.Empty) { continue; }
                        if (!savedMasterRowIds.Contains(owner))
                        {
                            throw new ForbiddenException(
                                $"Detail row in '{table.TableName}' belongs to a record this save does not " +
                                $"carry; record scope on model '{modelId}' cannot be confirmed for it.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The row versions whose <see cref="SysFields.MasterRowId"/> the repository would write for
        /// a row in the supplied state.
        /// </summary>
        /// <param name="state">The row state.</param>
        private static IEnumerable<DataRowVersion> WrittenVersions(DataRowState state) => state switch
        {
            DataRowState.Added => [DataRowVersion.Default],
            DataRowState.Deleted => [DataRowVersion.Original],
            DataRowState.Modified => [DataRowVersion.Original, DataRowVersion.Default],
            _ => [],
        };

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

            var authorization = Services.GetRequiredService<ICompanyAuthorizationService>();

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
