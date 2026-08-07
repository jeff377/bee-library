using System.Data;
using Bee.Base;
using Bee.Business.AuditLog;
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
            : base(ctx, accessToken, progId, isLocalCall)
        { }

        #endregion

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFunc"/>.
        /// </summary>
        protected override void DoExecFunc(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new FormExecFuncHandler(AccessToken);
            handler.InvokeExecFunc(ApiAccessRequirement.Authenticated, IsLocalCall, args, result);
        }

        /// <summary>
        /// Override to provide the implementation for <see cref="BusinessObject.ExecFuncAnonymous"/>.
        /// </summary>
        protected override void DoExecFuncAnonymous(ExecFuncArgs args, ExecFuncResult result)
        {
            var handler = new FormExecFuncHandler(AccessToken);
            handler.InvokeExecFunc(ApiAccessRequirement.Anonymous, IsLocalCall, args, result);
        }

        /// <summary>
        /// Retrieves list-view rows by executing the FormSchema-driven SELECT statement
        /// for <see cref="BusinessObject.ProgId"/>.
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
            // The user's zone travels as an argument rather than being resolved from ambient state:
            // this code path is shared with the client, and a helper that reads its zone from
            // somewhere invisible behaves differently on each side (ADR-032 D13).
            // `Get` yields null when the token has no session — blank then means UTC, which is the
            // defined fallback; adopting the server machine's zone instead is what D4 rules out.
            var dataSet = repository.GetNewData(ResolveSessionTimeZone());

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
        /// <remarks>
        /// <para>
        /// The pipeline, and where the database transaction sits in it:
        /// </para>
        /// <code>
        /// DoBeforeSave    outside the transaction
        /// DoSave          INSIDE the transaction
        /// change audit    outside the transaction
        /// DoAfterSave     outside the transaction
        /// </code>
        /// <para>
        /// Customise by overriding one of those three, not this method: the authorization and
        /// write-scope checks above them live here, and an override that replaces this method takes
        /// them over as well.
        /// </para>
        /// <para>
        /// Because the audit write sits outside the transaction, a record can persist while its
        /// change-audit entry fails. Raising the audit into the transaction would make <c>DoSave</c>
        /// more than persistence and require a transaction API at this layer; the framework accepts
        /// the gap instead.
        /// </para>
        /// </remarks>
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

            // One runner for the whole call: BeforeSave and AfterSave must see the same plugin
            // instances, so state computed in the first is still there in the second.
            var plugins = CreatePluginRunner();

            // Business extension point before persistence (field defaults / computation / validation).
            DoBeforeSave(context);
            // Plugins run after the step's final implementation — which may be an override in a
            // custom business object — so both extension routes can be used together.
            plugins.RunBeforeSave(context);

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
                WriteChangeAudit(changeKind, rowKey, AuditDiffGram.Serialize(changes), masterTableName, ProgId + ".Save");

            DoAfterSave(context);
            plugins.RunAfterSave(context);

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
        /// <remarks>
        /// <para>
        /// <b>Runs outside the database transaction.</b> The transaction covers
        /// <see cref="DoSave"/> alone, so nothing done here is rolled back by a later failure and
        /// nothing read here is protected from concurrent change.
        /// </para>
        /// <para>
        /// <b>Validation here has a time-of-check to time-of-use gap.</b> A read that finds stock
        /// sufficient can be invalidated by another transaction before <see cref="DoSave"/> runs,
        /// and the save still proceeds. Checks that must be atomic belong inside
        /// <see cref="DoSave"/>, expressed as a conditional UPDATE, a unique index or a check
        /// constraint. Reads here are for rejecting obviously wrong input, not for guarding against
        /// concurrency.
        /// </para>
        /// <para>
        /// To abort the save, throw
        /// <see cref="Bee.Base.Exceptions.UserMessageException"/> — the framework's
        /// business-flow interruption signal, which reaches the end user as its message.
        /// </para>
        /// </remarks>
        /// <param name="context">The save context.</param>
        protected virtual void DoBeforeSave(SaveContext context)
        {
            RuleProcessor.ApplyBeforeSave(context.Schema, context.DataSet, BuildRoundingContext(), ResolveSessionTimeZone());
        }

        /// <summary>
        /// Persists the data set. The base implementation dispatches INSERT / UPDATE / DELETE per row
        /// state through the repository and records the refreshed data set and affected-row counts on
        /// <paramref name="context"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The only step that runs inside the database transaction</b>, which the repository
        /// opens and commits within this call. Logic that must succeed or fail atomically with the
        /// record belongs here — in practice, inside a repository subclass whose own <c>Save</c>
        /// extends the same batch.
        /// </para>
        /// <para>
        /// Work added around <c>base.DoSave(context)</c> in an override is <b>not</b> in that
        /// transaction: the transaction is already committed when control returns.
        /// </para>
        /// </remarks>
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
        /// <remarks>
        /// <para>
        /// <b>Runs outside the database transaction, after it has committed.</b> Throwing here
        /// fails the call while the record stays saved, so the caller sees an error against data
        /// that is already persisted. Side effects placed here must therefore tolerate being
        /// retried, or be handed to a queue rather than performed inline — sending a notification
        /// synchronously and failing leaves nothing to retry from.
        /// </para>
        /// <para>
        /// This is also the right side of the boundary for anything that talks to the outside
        /// world. Holding a transaction open across an external call ties lock duration to that
        /// call's latency.
        /// </para>
        /// </remarks>
        /// <param name="context">The save context.</param>
        protected virtual void DoAfterSave(SaveContext context)
        {
        }

        /// <summary>
        /// Deletes a single master row directly by <c>RowId</c>.
        /// </summary>
        /// <remarks>
        /// Same shape as <see cref="Save(SaveArgs)"/>: <c>DoBeforeDelete</c> and
        /// <c>DoAfterDelete</c> run outside the database transaction, <c>DoDelete</c> inside it, and
        /// the delete audit is written between <c>DoDelete</c> and <c>DoAfterDelete</c> — outside.
        /// Customise by overriding one of those three rather than this method, which carries the
        /// authorization and record-scope resolution.
        /// </remarks>
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

            var plugins = CreatePluginRunner();

            // Snapshot the record (master + details) before deleting so the audit captures its full
            // before-image and BeforeDelete rules can evaluate against it. Load once, only when the
            // audit or a BeforeDelete rule needs it — the direct-delete path stays read-free otherwise.
            //
            // A delete-stage plugin counts too, and must: it is the only remaining view of what was
            // deleted, which a plugin propagating the deletion to another system needs. Leaving it
            // out would make the snapshot's presence depend on the change-audit switch, so the same
            // plugin would work in one deployment and see null in another.
            bool auditChange = ChangeAuditEnabled();
            bool pluginNeedsSnapshot = plugins.Chain.HasStage(FormPluginStage.BeforeDelete)
                || plugins.Chain.HasStage(FormPluginStage.AfterDelete);
            if (auditChange || pluginNeedsSnapshot || HasBeforeDeleteRules(schema))
                context.Snapshot = repository.GetData(args.RowId, scopeFilter);

            // Business extension point before deletion (BeforeDelete guard rules).
            DoBeforeDelete(context);
            plugins.RunBeforeDelete(context);

            DoDelete(context);

            if (auditChange && context.RowsAffected > 0)
                WriteDeleteAudit(context.Snapshot, args.RowId);

            DoAfterDelete(context);
            plugins.RunAfterDelete(context);

            return new DeleteResult { RowsAffected = context.RowsAffected };
        }

        /// <summary>
        /// Business extension point invoked before deletion, after authorization. The base
        /// implementation applies the schema-driven <c>BeforeDelete</c> guard rules against
        /// <see cref="DeleteContext.Snapshot"/>. Overrides should call
        /// <c>base.DoBeforeDelete(context)</c> first, then add custom logic.
        /// </summary>
        /// <remarks>
        /// <b>Runs outside the database transaction</b>, which covers <see cref="DoDelete"/> alone.
        /// The same time-of-check to time-of-use gap described on <see cref="DoBeforeSave"/> applies
        /// to any guard written here. To abort the delete, throw
        /// <see cref="Bee.Base.Exceptions.UserMessageException"/>.
        /// </remarks>
        /// <param name="context">The delete context.</param>
        protected virtual void DoBeforeDelete(DeleteContext context)
        {
            if (context.Snapshot != null)
                RuleProcessor.ApplyBeforeDelete(context.Schema, context.Snapshot, ResolveSessionTimeZone());
        }

        /// <summary>
        /// Deletes the record. The base implementation deletes the master row (cascading to details)
        /// through the repository and records the affected-row count on <paramref name="context"/>.
        /// </summary>
        /// <remarks>
        /// <b>The only step that runs inside the database transaction</b> (details then master, one
        /// batch). As with <see cref="DoSave"/>, work added around <c>base.DoDelete(context)</c> in
        /// an override is outside it.
        /// </remarks>
        /// <param name="context">The delete context.</param>
        protected virtual void DoDelete(DeleteContext context)
        {
            context.RowsAffected = context.Repository.Delete(context.Args.RowId, context.ScopeFilter);
        }

        /// <summary>
        /// Business extension point invoked after deletion and delete-audit write. The base
        /// implementation does nothing; override to run post-delete side effects.
        /// </summary>
        /// <remarks>
        /// <b>Runs outside the database transaction, after it has committed.</b> Throwing here
        /// fails the call while the record stays deleted; the retry and queueing guidance on
        /// <see cref="DoAfterSave"/> applies unchanged.
        /// </remarks>
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

        private IFormPluginResolver? _pluginResolver;

        /// <summary>
        /// Gets the resolver that maps this program to its business plugin chain.
        /// </summary>
        private IFormPluginResolver PluginResolver
            => _pluginResolver ??= Services.GetRequiredService<IFormPluginResolver>();

        /// <summary>
        /// Builds the plugin runner for one <c>Save</c> or <c>Delete</c> call.
        /// </summary>
        /// <remarks>
        /// One runner per call, which is what makes the plugin instances per operation: every stage
        /// of this call goes through the same runner and therefore the same objects.
        /// </remarks>
        private FormPluginRunner CreatePluginRunner()
            => PluginResolver.Resolve(GetCurrentCustomizeId(), ProgId)
                .CreateRunner(Context, AccessToken, ProgId);

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
        /// Resolves the requesting user's IANA time zone id, or an empty string when the token has
        /// no session (meaning UTC).
        /// </summary>
        private string ResolveSessionTimeZone()
            => SessionInfoService.Get(AccessToken)?.TimeZone ?? string.Empty;

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
