using System.Data;
using Bee.Base;
using Bee.Base.Data;
using Bee.Definition;
using Bee.Definition.Attributes;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Paging;
using Bee.Definition.Security;
using Bee.Definition.Settings;

namespace Bee.Business.Form
{
    /// <summary>
    /// The read side: list, lookup, new-row skeleton and single-record fetch.
    /// </summary>
    /// <remarks>
    /// Separated from the write side because none of it goes through the before/after hooks or the
    /// plugin pipeline — a reader looking for "what happens on save" should not have to pass through here.
    /// </remarks>
    public partial class FormBusinessObject
    {
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
    }
}
