using System.Globalization;
using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition.Paging;
using Bee.Definition.Sorting;
using Bee.Definition.Storage;
using Bee.Repository.Abstractions.Form;

namespace Bee.Repository.Form
{
    /// <summary>
    /// Repository implementation for data forms; executes FormSchema-driven SELECT
    /// statements against the master table via the dialect-specific
    /// <c>IFormCommandBuilder</c>.
    /// </summary>
    public class DataFormRepository : IDataFormRepository
    {
        private readonly FormSchema _schema;
        private readonly IDefineAccess _defineAccess;
        private readonly IDbAccessFactory _dbAccessFactory;
        private readonly IDbConnectionManager _connectionManager;
        private readonly string _databaseId;

        /// <summary>
        /// Initializes a new instance of <see cref="DataFormRepository"/>.
        /// </summary>
        /// <param name="progId">The program identifier (also the master table name).</param>
        /// <param name="schema">The resolved form schema for <paramref name="progId"/>.</param>
        /// <param name="defineAccess">
        /// The define access service, forwarded to <c>IFormCommandBuilder</c> to resolve
        /// relation-form schemas during SELECT construction.
        /// </param>
        /// <param name="dbAccessFactory">The DI-resolved database access factory.</param>
        /// <param name="connectionManager">
        /// The DI-resolved connection manager, used to look up the database type for
        /// dialect routing.
        /// </param>
        /// <param name="databaseId">The database identifier used for connection and dialect resolution.</param>
        public DataFormRepository(
            string progId,
            FormSchema schema,
            IDefineAccess defineAccess,
            IDbAccessFactory dbAccessFactory,
            IDbConnectionManager connectionManager,
            string databaseId)
        {
            ProgId = progId ?? throw new ArgumentNullException(nameof(progId));
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _defineAccess = defineAccess ?? throw new ArgumentNullException(nameof(defineAccess));
            _dbAccessFactory = dbAccessFactory ?? throw new ArgumentNullException(nameof(dbAccessFactory));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _databaseId = databaseId ?? throw new ArgumentNullException(nameof(databaseId));
        }

        /// <summary>
        /// Gets the program identifier.
        /// </summary>
        public string ProgId { get; }

        /// <summary>
        /// The framework-wide upper bound for <see cref="PagingOptions.PageSize"/>.
        /// Values above this cap are clamped on the server to prevent callers from
        /// accidentally loading huge result sets (for example via <c>int.MaxValue</c>).
        /// </summary>
        private const int MaxPageSize = 1000;

        /// <inheritdoc/>
        public DataFormListResult GetList(
            string selectFields,
            FilterNode? filter,
            SortFieldCollection? sortFields,
            PagingOptions? paging = null)
        {
            // FormSchema.MasterTable.TableName == ProgId (framework invariant), so we
            // pass ProgId directly as the target table name.
            var resolvedSelectFields = StringUtilities.IsNotEmpty(selectFields)
                ? selectFields
                : _schema.ListFields;  // empty falls through to SelectCommandBuilder as "all fields"

            var connInfo = _connectionManager.GetConnectionInfo(_databaseId);
            var builder = DbDialectRegistry.Get(connInfo.DatabaseType)
                .CreateFormCommandBuilder(_schema, _defineAccess);
            var dbAccess = _dbAccessFactory.Create(_databaseId);

            if (paging == null)
            {
                var spec = builder.BuildSelect(ProgId, resolvedSelectFields, filter, sortFields);
                return new DataFormListResult { Table = dbAccess.Execute(spec).Table };
            }

            // Paged path: clamp PageSize, supply a deterministic ORDER BY, run optional
            // COUNT, then the paged SELECT. When IncludeTotalCount is false we take an
            // extra probe row (PageSize + 1) to compute HasMore without a COUNT round-trip.
            var pageSize = Math.Clamp(paging.PageSize, 1, MaxPageSize);
            var page = Math.Max(paging.Page, 1);
            var skip = (page - 1) * pageSize;

            // Paging requires a deterministic ORDER BY. SQL Server and Oracle reject
            // OFFSET/FETCH without ORDER BY; PG/SQLite/MySQL allow it but return rows
            // in undefined order. Falling back here is the Repository's job — the SQL
            // layer does not know about the `sys_no` convention.
            var effectiveSort = sortFields ?? DefaultSortForPaging(_schema);

            int? totalCount = null;
            if (paging.IncludeTotalCount)
            {
                var countSpec = builder.BuildCount(ProgId, filter);
                totalCount = Convert.ToInt32(dbAccess.Execute(countSpec).Scalar, CultureInfo.InvariantCulture);
            }

            int take = paging.IncludeTotalCount ? pageSize : pageSize + 1;
            var pagedSpec = builder.BuildSelect(ProgId, resolvedSelectFields, filter, effectiveSort, skip, take);
            var table = dbAccess.Execute(pagedSpec).Table!;

            bool hasMore;
            if (paging.IncludeTotalCount)
            {
                hasMore = totalCount > skip + table.Rows.Count;
            }
            else
            {
                hasMore = table.Rows.Count > pageSize;
                if (hasMore)
                {
                    // Trim the probe row so the caller never sees the extra record.
                    table.Rows.RemoveAt(table.Rows.Count - 1);
                }
            }

            return new DataFormListResult
            {
                Table = table,
                Paging = new PagingInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    HasMore = hasMore,
                },
            };
        }

        /// <summary>
        /// Returns the default sort applied to paged queries when the caller does not
        /// supply a <see cref="SortFieldCollection"/>. Uses <c>sys_no ASC</c> when the
        /// master table defines it; otherwise throws to force the caller to provide
        /// an explicit sort (Guid-based <c>sys_rowid</c> would be deterministic but
        /// meaningless to humans).
        /// </summary>
        private static SortFieldCollection DefaultSortForPaging(FormSchema schema)
        {
            var masterTable = schema.MasterTable
                ?? throw new InvalidOperationException(
                    $"Schema '{schema.ProgId}' has no master table; cannot derive a default paging sort.");

            if (masterTable.Fields == null || !masterTable.Fields.Contains(SysFields.No))
            {
                throw new InvalidOperationException(
                    $"Cannot derive a default paging sort for schema '{schema.ProgId}': " +
                    $"the master table does not define '{SysFields.No}'. " +
                    $"Supply an explicit SortFields when calling GetList with paging.");
            }

            return [new SortField(SysFields.No, SortDirection.Asc)];
        }
    }
}
