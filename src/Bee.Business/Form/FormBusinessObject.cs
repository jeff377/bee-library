using System.Data;
using System.Globalization;
using Bee.Base;
using Bee.Base.Data;
using Bee.Base.Exceptions;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Paging;
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
        /// Retrieves lookup candidate rows for picker windows that reference this form.
        /// The projection is the server-resolved lookup field set
        /// (see <c>FormSchema.GetLookupFields</c>) prefixed with <c>sys_rowid</c>;
        /// the caller cannot widen it.
        /// </summary>
        /// <param name="args">The input arguments.</param>
        /// <remarks>
        /// Unlike <see cref="GetList"/>, this action is intentionally not gated by the
        /// form's <c>Read</c> permission: a user who may not browse the target form's
        /// list still needs to pick a reference value from it. Exposure is bounded by
        /// the <c>FormSchema.LookupFields</c> declaration. Override
        /// <see cref="GetLookupFilter"/> to constrain the candidate rows (e.g. active
        /// records only). When <see cref="GetLookupArgs.Paging"/> is <c>null</c> a
        /// default page size of 100 is applied.
        /// </remarks>
        [ApiAccessControl(ApiProtectionLevel.Public, ApiAccessRequirement.Authenticated)]
        public virtual GetLookupResult GetLookup(GetLookupArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            var schema = DefineAccess.GetFormSchema(ProgId);
            var lookupFields = schema.GetLookupFields();
            var selectFields = string.Join(",",
                lookupFields.Select(f => f.FieldName).Prepend(SysFields.RowId));
            var filter = CombineWithScope(
                BuildLookupSearchFilter(lookupFields, args.SearchText),
                GetLookupFilter());
            var paging = args.Paging ?? new PagingOptions { PageSize = DefaultLookupPageSize };

            var repository = CreateDataFormRepository(ProgId);
            var listResult = repository.GetList(selectFields, filter, null, paging);

            return new GetLookupResult
            {
                Table = listResult.Table,
                Paging = listResult.Paging,
            };
        }

        /// <summary>
        /// Override to constrain lookup candidate rows with a business filter
        /// (e.g. active records only). The default returns <c>null</c> (no constraint);
        /// a non-null filter is AND-combined with the search filter.
        /// </summary>
        protected virtual FilterNode? GetLookupFilter() => null;

        /// <summary>
        /// Builds the OR-combined LIKE filter that matches <paramref name="searchText"/>
        /// against the string-typed lookup fields; <c>null</c> when the text is empty or
        /// no string-typed field exists.
        /// </summary>
        private static FilterNode? BuildLookupSearchFilter(
            IReadOnlyList<FormField> lookupFields, string searchText)
        {
            if (StringUtilities.IsEmpty(searchText)) { return null; }

            var conditions = lookupFields
                .Where(f => f.DbType == FieldDbType.String)
                .Select(f => (FilterNode)FilterCondition.Contains(f.FieldName, searchText))
                .ToArray();
            return conditions.Length switch
            {
                0 => null,
                1 => conditions[0],
                _ => FilterGroup.Any(conditions),
            };
        }

        /// <summary>
        /// Default page size applied to lookup queries when the caller omits paging,
        /// so an unbounded lookup never loads a large table into memory.
        /// </summary>
        private const int DefaultLookupPageSize = 100;

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
            // Layer-2 record scope is re-checked only when an existing master record is saved — any
            // master row state other than Added. (A save that only changes details leaves the master
            // Unchanged but still modifies the record, so it counts as an Update; Added is a Create,
            // governed by the action grant. Scope is master-only, so once the master passes the whole
            // record persists with it.)
            if (HasExistingMasterWrite(args.DataSet))
                EnforceWriteScope(args.DataSet, repository);

            // Capture the change set (before/after) and the master key/kind before Save, because
            // the ADO.NET adapter calls AcceptChanges on success and discards RowState / original
            // values. Only pay the cost when change auditing is enabled.
            var masterTableName = DefineAccess.GetFormSchema(ProgId).MasterTable?.TableName ?? string.Empty;
            bool auditChange = ChangeAuditEnabled();
            using var changes = auditChange ? args.DataSet.GetChanges() : null;
            var (rowKey, changeKind) = auditChange
                ? ExtractMasterChange(args.DataSet, masterTableName)
                : (null, ChangeKind.Update);

            var (refreshed, affected) = repository.Save(args.DataSet);

            if (auditChange && changes != null)
                WriteChangeAudit(changeKind, rowKey, SerializeDiffGram(changes), masterTableName, ProgId + ".Save");

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
            var scopeFilter = ResolveScopeFilter(PermissionAction.Delete);

            // Snapshot the record (master + details) before deleting so the audit captures its full
            // before-image. Only load when change auditing is enabled — the direct-delete path stays
            // read-free otherwise.
            bool auditChange = ChangeAuditEnabled();
            var snapshot = auditChange ? repository.GetData(args.RowId, scopeFilter) : null;

            var rowsAffected = repository.Delete(args.RowId, scopeFilter);

            if (auditChange && rowsAffected > 0)
                WriteDeleteAudit(snapshot, args.RowId);

            return new DeleteResult { RowsAffected = rowsAffected };
        }

        #region 異動記錄（audit trail）

        /// <summary>
        /// Whether data-change auditing is enabled (global + change category). Resolved through the
        /// <see cref="IBeeContext.Services"/> escape hatch; false gates out all capture work.
        /// </summary>
        private bool ChangeAuditEnabled()
            => Services.GetService<AuditLogOptions>() is { Enabled: true, ChangeEnabled: true };

        /// <summary>
        /// Reads the master row's key and derives the <see cref="ChangeKind"/> from its state. Must be
        /// called before <c>Save</c> applies the changes (which resets RowState).
        /// </summary>
        private static (string? rowKey, ChangeKind kind) ExtractMasterChange(DataSet dataSet, string masterTableName)
        {
            if (string.IsNullOrEmpty(masterTableName) || !dataSet.Tables.Contains(masterTableName))
                return (null, ChangeKind.Update);

            var table = dataSet.Tables[masterTableName]!;
            if (table.Rows.Count == 0 || !table.Columns.Contains(SysFields.RowId))
                return (null, ChangeKind.Update);

            var row = table.Rows[0];
            var kind = row.RowState switch
            {
                DataRowState.Added => ChangeKind.Insert,
                DataRowState.Deleted => ChangeKind.Delete,
                _ => ChangeKind.Update,
            };
            var version = row.RowState == DataRowState.Deleted ? DataRowVersion.Original : DataRowVersion.Current;
            return (ValueUtilities.CStr(row[SysFields.RowId, version]), kind);
        }

        /// <summary>
        /// Serialises the changed rows to a DataSet DiffGram, which carries both the current and the
        /// original (before) values. Plain <c>WriteXml</c> would only write current values.
        /// </summary>
        private static string SerializeDiffGram(DataSet changes)
        {
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            changes.WriteXml(writer, XmlWriteMode.DiffGram);
            return writer.ToString();
        }

        /// <summary>
        /// Writes the delete audit. When the pre-delete <paramref name="snapshot"/> is available its
        /// rows are marked deleted and serialised as a DiffGram before-image (full deleted content);
        /// otherwise the deleted key alone is recorded.
        /// </summary>
        private void WriteDeleteAudit(DataSet? snapshot, Guid rowId)
        {
            var masterTableName = DefineAccess.GetFormSchema(ProgId).MasterTable?.TableName ?? string.Empty;
            var rowKey = rowId.ToString();

            string xml = MinimalDeleteXml(masterTableName, rowKey);
            if (snapshot != null && HasAnyRows(snapshot))
            {
                MarkAllRowsDeleted(snapshot);
                using var changes = snapshot.GetChanges();
                if (changes != null)
                    xml = SerializeDiffGram(changes);
            }

            WriteChangeAudit(ChangeKind.Delete, rowKey, xml, masterTableName, ProgId + ".Delete");
        }

        /// <summary>Marks every row in every table as deleted so <c>GetChanges</c> yields the before-image.</summary>
        private static void MarkAllRowsDeleted(DataSet dataSet)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                // Iterate backwards: Delete() on an Added row removes it immediately; loaded rows are
                // Unchanged so this is defensive.
                for (int i = table.Rows.Count - 1; i >= 0; i--)
                {
                    var row = table.Rows[i];
                    if (row.RowState != DataRowState.Deleted)
                        row.Delete();
                }
            }
        }

        private static bool HasAnyRows(DataSet dataSet)
        {
            foreach (DataTable table in dataSet.Tables)
            {
                if (table.Rows.Count > 0)
                    return true;
            }
            return false;
        }

        private static string MinimalDeleteXml(string masterTableName, string rowKey)
            => $"<DeletedRow table=\"{masterTableName}\" sys_rowid=\"{rowKey}\" />";

        /// <summary>
        /// Builds a <see cref="ChangeAuditEntry"/> from the session (denormalised who / company) and
        /// the supplied change payload, and writes it best-effort through <see cref="IAuditLogWriter"/>.
        /// </summary>
        private void WriteChangeAudit(ChangeKind changeKind, string? rowKey, string changesXml, string masterTableName, string source)
        {
            var session = SessionInfoService.Get(AccessToken);
            var companyId = session?.CompanyId;
            string? companyName = null;
            if (!string.IsNullOrEmpty(companyId))
                companyName = Services.GetService<ICompanyInfoService>()?.Get(companyId)?.CompanyName;

            Services.GetService<IAuditLogWriter>()?.Write(new ChangeAuditEntry
            {
                UserId = session?.UserId,
                UserName = session?.UserName,
                CompanyId = companyId,
                CompanyName = companyName,
                AccessToken = AccessToken,
                ProgId = ProgId,
                ChangeTableName = masterTableName,
                RowKey = rowKey,
                ChangeKind = changeKind,
                IsSensitive = false,
                ChangesXml = changesXml,
                Source = source,
            });
        }

        #endregion

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
