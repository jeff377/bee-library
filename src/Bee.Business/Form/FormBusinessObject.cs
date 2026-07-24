using System.Data;
using Bee.Base;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Identity;
using Bee.Definition.Logging;
using Bee.Definition.Paging;
using Bee.Definition.Security;
using Bee.Definition.Settings;

namespace Bee.Business.Form
{
    /// <summary>
    /// Form-level business logic object.
    /// </summary>
    public partial class FormBusinessObject : BusinessObject, IFormBusinessObject
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

            // Record the detail view (who viewed which record). Opt-in and best-effort; field-level
            // detail is intentionally not recorded — a detail view loads the whole record.
            if (dataSet != null && AccessAuditEnabled())
                WriteAccessAudit(args.RowId, ProgId + ".GetData");

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

            var schema = DefineAccess.GetFormSchema(ProgId);
            var context = new SaveContext(args, args.DataSet, repository, schema);

            // Business extension point before persistence (field defaults / computation / validation).
            DoBeforeSave(context);

            // Capture the change set (before/after) and the master key/kind before persistence,
            // because the ADO.NET adapter calls AcceptChanges on success and discards RowState /
            // original values. Runs after DoBeforeSave so the audit reflects any computed values.
            // Only pay the cost when change auditing is enabled.
            var masterTableName = schema.MasterTable?.TableName ?? string.Empty;
            bool auditChange = ChangeAuditEnabled();
            using var changes = auditChange ? args.DataSet.GetChanges() : null;
            var (rowKey, changeKind) = auditChange
                ? ExtractMasterChange(args.DataSet, masterTableName)
                : (null, ChangeKind.Update);

            DoSave(context);

            if (auditChange && changes != null)
                WriteChangeAudit(changeKind, rowKey, SerializeDiffGram(changes), masterTableName, ProgId + ".Save");

            DoAfterSave(context);

            return new SaveResult
            {
                DataSet = context.RefreshedDataSet,
                AffectedRows = context.AffectedRows,
            };
        }

        /// <summary>
        /// Business extension point invoked before persistence, after authorization and write-scope
        /// checks. The base implementation applies the schema-driven rule engine (default-value and
        /// computed-field expressions, then <c>BeforeSave</c> validation rules). Overrides should call
        /// <c>base.DoBeforeSave(context)</c> first, then add custom logic.
        /// </summary>
        /// <param name="context">The save context.</param>
        protected virtual void DoBeforeSave(SaveContext context)
        {
            RuleProcessor.ApplyBeforeSave(context.Schema, context.DataSet, BuildRoundingContext());
        }

        /// <summary>
        /// Persists the data set. The base implementation dispatches INSERT / UPDATE / DELETE per row
        /// state through the repository and records the refreshed data set and affected-row counts on
        /// <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The save context.</param>
        protected virtual void DoSave(SaveContext context)
        {
            var (refreshed, affected) = context.Repository.Save(context.DataSet);
            context.RefreshedDataSet = refreshed;
            context.AffectedRows = affected;
        }

        /// <summary>
        /// Business extension point invoked after persistence and change-audit write. The base
        /// implementation does nothing; override to run post-save side effects.
        /// </summary>
        /// <param name="context">The save context.</param>
        protected virtual void DoAfterSave(SaveContext context)
        {
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
            var schema = DefineAccess.GetFormSchema(ProgId);
            var context = new DeleteContext(args, repository, scopeFilter, schema);

            // Snapshot the record (master + details) before deleting so the audit captures its full
            // before-image and BeforeDelete rules can evaluate against it. Load once, only when the
            // audit or a BeforeDelete rule needs it — the direct-delete path stays read-free otherwise.
            bool auditChange = ChangeAuditEnabled();
            if (auditChange || HasBeforeDeleteRules(schema))
                context.Snapshot = repository.GetData(args.RowId, scopeFilter);

            // Business extension point before deletion (BeforeDelete guard rules).
            DoBeforeDelete(context);

            DoDelete(context);

            if (auditChange && context.RowsAffected > 0)
                WriteDeleteAudit(context.Snapshot, args.RowId);

            DoAfterDelete(context);

            return new DeleteResult { RowsAffected = context.RowsAffected };
        }

        /// <summary>
        /// Business extension point invoked before deletion, after authorization. The base
        /// implementation applies the schema-driven <c>BeforeDelete</c> guard rules against
        /// <see cref="DeleteContext.Snapshot"/>. Overrides should call
        /// <c>base.DoBeforeDelete(context)</c> first, then add custom logic.
        /// </summary>
        /// <param name="context">The delete context.</param>
        protected virtual void DoBeforeDelete(DeleteContext context)
        {
            if (context.Snapshot != null)
                RuleProcessor.ApplyBeforeDelete(context.Schema, context.Snapshot);
        }

        /// <summary>
        /// Deletes the record. The base implementation deletes the master row (cascading to details)
        /// through the repository and records the affected-row count on <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The delete context.</param>
        protected virtual void DoDelete(DeleteContext context)
        {
            context.RowsAffected = context.Repository.Delete(context.Args.RowId, context.ScopeFilter);
        }

        /// <summary>
        /// Business extension point invoked after deletion and delete-audit write. The base
        /// implementation does nothing; override to run post-delete side effects.
        /// </summary>
        /// <param name="context">The delete context.</param>
        protected virtual void DoAfterDelete(DeleteContext context)
        {
        }

        private IFormRuleProcessor? _ruleProcessor;

        /// <summary>
        /// Gets the rule processor that evaluates this form's field expressions and rules.
        /// </summary>
        private IFormRuleProcessor RuleProcessor
            => _ruleProcessor ??= Services.GetRequiredService<IFormRuleProcessor>();

        /// <summary>
        /// Returns true when the schema declares any enabled <c>BeforeDelete</c> rule.
        /// </summary>
        private static bool HasBeforeDeleteRules(FormSchema schema)
            => schema.Rules != null &&
               schema.Rules.Any(r => r.Enabled && r.Trigger == FormRuleTrigger.BeforeDelete);

        /// <summary>
        /// Builds the rounding context used to round computed numeric fields, from the current
        /// session's company and the currency/unit settings.
        /// </summary>
        private RoundingContext BuildRoundingContext()
        {
            return new RoundingContext
            {
                Company = ResolveCompanyInfo(),
                CurrencySettings = DefineAccess.GetCurrencySettings(),
                UnitSettings = DefineAccess.GetUnitSettings(),
            };
        }

        /// <summary>
        /// Resolves the current session's <see cref="CompanyInfo"/>, or null when no company is bound.
        /// </summary>
        private CompanyInfo? ResolveCompanyInfo()
        {
            var session = SessionInfoService.Get(AccessToken);
            if (session == null || string.IsNullOrEmpty(session.CompanyId)) { return null; }
            return Services.GetService<ICompanyInfoService>()?.Get(session.CompanyId);
        }
    }
}
