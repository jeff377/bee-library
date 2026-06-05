using System.Data;
using Bee.Base;
using Bee.Base.Exceptions;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Filters;
using Bee.Definition.Identity;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.Form;

namespace Bee.Business.Form
{
    /// <summary>
    /// Form-level business logic object.
    /// </summary>
    public class FormBusinessObject : BusinessObject, IFormBusinessObject
    {
        #region 建構函式

        /// <summary>
        /// Initializes a new instance of the <see cref="FormBusinessObject"/> class.
        /// </summary>
        /// <param name="ctx">The per-call context aggregating cross-cutting services.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="progId">The program identifier.</param>
        /// <param name="isLocalCall">Whether the call originates from a local source.</param>
        public FormBusinessObject(IBeeContext ctx, Guid accessToken, string progId, bool isLocalCall = true)
            : base(ctx, accessToken, isLocalCall)
        {
            ProgId = progId;
        }

        #endregion

        /// <summary>
        /// Gets the program identifier.
        /// </summary>
        public string ProgId { get; }

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFunc"/>.
        /// </summary>
        protected override void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new FormExecFuncHandler(AccessToken);
            handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, args, result);
        }

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFuncAnonymous"/>.
        /// </summary>
        protected override void DoExecFuncAnonymous(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new FormExecFuncHandler(AccessToken);
            handler.InvokeExecFunc(ApiAccessRequirement.Anonymous, args, result);
        }

        /// <summary>
        /// Retrieves list-view rows by executing the FormSchema-driven SELECT statement
        /// for <see cref="ProgId"/>.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <remarks>
        /// When <see cref="GetListArgs.Paging"/> is <c>null</c> the query is unpaged
        /// and callers should supply a <c>Filter</c> that bounds the result set,
        /// otherwise an unbounded query against a large table loads every matching
        /// row into memory on both the server and the client. Set <c>Paging</c> to
        /// page through large result sets.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetListResult GetList(GetListArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            Authorize(PermissionAction.Read);

            var filter = CombineWithScope(args.Filter, ResolveScopeFilter(PermissionAction.Read));
            var repository = CreateDataFormRepository(ProgId);
            var listResult = repository.GetList(args.SelectFields, filter, args.SortFields, args.Paging);

            return new GetListResult
            {
                Table = listResult.Table,
                Paging = listResult.Paging,
            };
        }

        /// <summary>
        /// Returns a blank <c>DataSet</c> skeleton seeded with FormSchema
        /// defaults and a server-issued <c>sys_rowid</c>.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetNewDataResult GetNewData(GetNewDataArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            Authorize(PermissionAction.Read);

            var repository = CreateDataFormRepository(ProgId);
            var dataSet = repository.GetNewData();

            return new GetNewDataResult { DataSet = dataSet };
        }

        /// <summary>
        /// Loads a single master row (and its details) by <c>RowId</c>.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetDataResult GetData(GetDataArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            Authorize(PermissionAction.Read);

            var repository = CreateDataFormRepository(ProgId);
            var dataSet = repository.GetData(args.RowId, ResolveScopeFilter(PermissionAction.Read));

            return new GetDataResult { DataSet = dataSet };
        }

        /// <summary>
        /// Persists a <c>DataSet</c> by dispatching INSERT / UPDATE / DELETE
        /// based on each row's <c>RowState</c>.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual SaveResult Save(SaveArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (args.DataSet == null)
                throw new ArgumentException("Save requires a non-null DataSet.", nameof(args));
            AuthorizeSave(args.DataSet);

            var repository = CreateDataFormRepository(ProgId);
            EnforceWriteScope(args.DataSet, repository);
            var (refreshed, affected) = repository.Save(args.DataSet);

            return new SaveResult
            {
                DataSet = refreshed,
                AffectedRows = affected,
            };
        }

        /// <summary>
        /// Deletes a single master row directly by <c>RowId</c>.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual DeleteResult Delete(DeleteArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            Authorize(PermissionAction.Delete);

            var repository = CreateDataFormRepository(ProgId);
            var rowsAffected = repository.Delete(args.RowId, ResolveScopeFilter(PermissionAction.Delete));

            return new DeleteResult { RowsAffected = rowsAffected };
        }

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
        /// Enforces layer-2 record scope on writes by authoritatively re-querying each mutated master
        /// row. For every <c>Modified</c> (Update) / <c>Deleted</c> (Delete) master row, confirms the
        /// target row is in the caller's scope against the database — not the supplied payload, so a
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
                var action = row.RowState switch
                {
                    DataRowState.Modified => PermissionAction.Update,
                    DataRowState.Deleted => PermissionAction.Delete,
                    _ => PermissionAction.None,  // Added (Create) / Unchanged — not write-scoped
                };
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
