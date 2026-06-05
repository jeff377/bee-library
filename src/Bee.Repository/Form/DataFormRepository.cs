using System.Data;
using System.Globalization;
using Bee.Base;
using Bee.Base.Data;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition;
using Bee.Definition.Database;
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

        /// <inheritdoc/>
        public DataSet GetNewData()
        {
            var dataSet = new DataSet(ProgId);

            var masterTable = _schema.MasterTable
                ?? throw new InvalidOperationException(
                    $"FormSchema '{ProgId}' has no master table; cannot build a new-data skeleton.");

            // Master skeleton + one Added row seeded with FormSchema defaults
            // and a server-issued sys_rowid.
            var masterDataTable = BuildEmptyDataTable(masterTable);
            dataSet.Tables.Add(masterDataTable);

            var masterRow = masterDataTable.NewRow();
            ApplyMasterDefaults(masterRow, masterTable);
            masterRow[SysFields.RowId] = Guid.NewGuid();
            masterDataTable.Rows.Add(masterRow);

            foreach (var detail in EnumerateDetailTables())
                dataSet.Tables.Add(BuildEmptyDataTable(detail));

            return dataSet;
        }

        /// <inheritdoc/>
        public DataSet? GetData(Guid rowId, FilterNode? scopeFilter = null)
        {
            var connInfo = _connectionManager.GetConnectionInfo(_databaseId);
            var builder = DbDialectRegistry.Get(connInfo.DatabaseType)
                .CreateFormCommandBuilder(_schema, _defineAccess);
            var dbAccess = _dbAccessFactory.Create(_databaseId);

            // Master row by sys_rowid, AND-combined with the record-scope filter when supplied so
            // an out-of-scope row reads as "not found" (null).
            var masterFilter = CombineWithScope(FilterCondition.Equal(SysFields.RowId, rowId), scopeFilter);
            var masterSpec = builder.BuildSelect(ProgId, string.Empty, masterFilter);
            var masterDataTable = dbAccess.Execute(masterSpec).Table;
            if (masterDataTable == null || masterDataTable.Rows.Count == 0)
                return null;

            masterDataTable.TableName = ProgId;

            var dataSet = new DataSet(ProgId);
            dataSet.Tables.Add(masterDataTable);

            var masterRowId = CoerceToGuid(masterDataTable.Rows[0][SysFields.RowId]);
            var detailFilter = FilterCondition.Equal(SysFields.MasterRowId, masterRowId);

            foreach (var detailTableName in EnumerateDetailTables().Select(detail => detail.TableName))
            {
                var detailSpec = builder.BuildSelect(detailTableName, string.Empty, detailFilter);
                var detailDataTable = dbAccess.Execute(detailSpec).Table ?? new DataTable(detailTableName);
                detailDataTable.TableName = detailTableName;
                dataSet.Tables.Add(detailDataTable);
            }

            dataSet.AcceptChanges();
            return dataSet;
        }

        /// <inheritdoc/>
        public (DataSet? Refreshed, Dictionary<string, int> AffectedRows) Save(DataSet dataSet)
        {
            ArgumentNullException.ThrowIfNull(dataSet);

            var connInfo = _connectionManager.GetConnectionInfo(_databaseId);
            var builder = DbDialectRegistry.Get(connInfo.DatabaseType)
                .CreateFormCommandBuilder(_schema, _defineAccess);

            var affected = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var batch = new DbBatchSpec { UseTransaction = true };

            // Determine the order in which tables are processed. Detail
            // deletions run before master deletion to avoid FK violations;
            // inserts/updates use master-first order so detail rows can
            // reference an already-persisted master.
            var masterTable = _schema.MasterTable
                ?? throw new InvalidOperationException(
                    $"FormSchema '{ProgId}' has no master table; cannot Save.");
            var detailTables = EnumerateDetailTables().ToList();

            // First pass: per-table deletes (details before master).
            foreach (var detail in detailTables)
                CollectRowCommands(builder, dataSet, detail.TableName, batch, affected, includeDelete: true,
                                   includeInsertUpdate: false);
            CollectRowCommands(builder, dataSet, masterTable.TableName, batch, affected, includeDelete: true,
                               includeInsertUpdate: false);

            // Second pass: master insert/update first, then details.
            CollectRowCommands(builder, dataSet, masterTable.TableName, batch, affected, includeDelete: false,
                               includeInsertUpdate: true);
            foreach (var detail in detailTables)
                CollectRowCommands(builder, dataSet, detail.TableName, batch, affected, includeDelete: false,
                                   includeInsertUpdate: true);

            if (batch.Commands.Count == 0)
                throw new InvalidOperationException(
                    "DataSet has no pending changes; Save would be a no-op.");

            var dbAccess = _dbAccessFactory.Create(_databaseId);
            dbAccess.ExecuteBatch(batch);

            // Re-fetch the saved master so server-generated columns surface
            // back to the caller.
            var masterRowId = ExtractMasterRowId(dataSet, masterTable.TableName);
            DataSet? refreshed = masterRowId.HasValue ? GetData(masterRowId.Value) : null;

            return (refreshed, affected);
        }

        /// <inheritdoc/>
        public int Delete(Guid rowId)
        {
            var connInfo = _connectionManager.GetConnectionInfo(_databaseId);
            var builder = DbDialectRegistry.Get(connInfo.DatabaseType)
                .CreateFormCommandBuilder(_schema, _defineAccess);

            var batch = new DbBatchSpec { UseTransaction = true };

            // Cascade delete details first (FK on sys_master_rowid), then master.
            var detailFilter = FilterCondition.Equal(SysFields.MasterRowId, rowId);
            foreach (var detail in EnumerateDetailTables())
                batch.Commands.Add(builder.BuildDelete(detail.TableName, detailFilter));

            var masterFilter = FilterCondition.Equal(SysFields.RowId, rowId);
            batch.Commands.Add(builder.BuildDelete(ProgId, masterFilter));

            var dbAccess = _dbAccessFactory.Create(_databaseId);
            var result = dbAccess.ExecuteBatch(batch);

            // The master DELETE is the last command; its RowsAffected drives
            // the caller-visible count.
            var lastIndex = result.Results.Count - 1;
            return lastIndex >= 0 ? result.Results[lastIndex].RowsAffected : 0;
        }

        private IEnumerable<FormTable> EnumerateDetailTables()
        {
            if (_schema.Tables == null)
                yield break;

            foreach (FormTable table in _schema.Tables)
            {
                if (string.Equals(table.TableName, ProgId, StringComparison.OrdinalIgnoreCase))
                    continue;
                yield return table;
            }
        }

        private static DataTable BuildEmptyDataTable(FormTable formTable)
        {
            var dataTable = new DataTable(formTable.TableName);
            if (formTable.Fields == null)
                return dataTable;

            foreach (FormField field in formTable.Fields)
            {
                // Skip virtual and relation fields — they are not part of the
                // underlying table and have no persistent column.
                if (field.Type != FieldType.DbField)
                    continue;
                dataTable.AddColumn(field.FieldName, field.DbType);
            }

            return dataTable;
        }

        private static void ApplyMasterDefaults(DataRow row, FormTable formTable)
        {
            if (formTable.Fields == null)
                return;

            foreach (FormField field in formTable.Fields)
            {
                if (field.Type != FieldType.DbField)
                    continue;
                if (!row.Table.Columns.Contains(field.FieldName))
                    continue;
                if (StringUtilities.IsEmpty(field.DefaultValue))
                    continue;

                var column = row.Table.Columns[field.FieldName]!;
                row[field.FieldName] = ConvertDefaultValue(field.DefaultValue, column.DataType);
            }
        }

        private static object ConvertDefaultValue(string raw, Type targetType)
        {
            if (targetType == typeof(string))
                return raw;
            if (targetType == typeof(Guid))
                return Guid.TryParse(raw, out var g) ? g : Guid.Empty;
            try
            {
                return Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return DBNull.Value;
            }
            catch (InvalidCastException)
            {
                return DBNull.Value;
            }
        }

        private static void CollectRowCommands(
            Bee.Db.Dml.IFormCommandBuilder builder,
            DataSet dataSet,
            string tableName,
            DbBatchSpec batch,
            Dictionary<string, int> affected,
            bool includeDelete,
            bool includeInsertUpdate)
        {
            if (!dataSet.Tables.Contains(tableName))
                return;

            var table = dataSet.Tables[tableName]!;
            int tableCount = affected.TryGetValue(tableName, out var existing) ? existing : 0;

            foreach (DataRow row in table.Rows)
            {
                switch (row.RowState)
                {
                    case DataRowState.Added when includeInsertUpdate:
                        batch.Commands.Add(builder.BuildInsert(tableName, row));
                        tableCount++;
                        break;
                    case DataRowState.Modified when includeInsertUpdate:
                        batch.Commands.Add(builder.BuildUpdate(tableName, row));
                        tableCount++;
                        break;
                    case DataRowState.Deleted when includeDelete:
                        var rowId = CoerceToGuid(row[SysFields.RowId, DataRowVersion.Original]);
                        batch.Commands.Add(builder.BuildDelete(tableName,
                            FilterCondition.Equal(SysFields.RowId, rowId)));
                        tableCount++;
                        break;
                    default:
                        break;
                }
            }

            affected[tableName] = tableCount;
        }

        private static Guid? ExtractMasterRowId(DataSet dataSet, string masterTableName)
        {
            if (!dataSet.Tables.Contains(masterTableName))
                return null;

            var table = dataSet.Tables[masterTableName]!;
            foreach (DataRow row in table.Rows)
            {
                if (row.RowState == DataRowState.Deleted)
                    continue;
                if (!row.Table.Columns.Contains(SysFields.RowId))
                    continue;
                var coerced = TryCoerceToGuid(row[SysFields.RowId]);
                if (coerced.HasValue && coerced.Value != Guid.Empty)
                    return coerced.Value;
            }
            return null;
        }

        /// <summary>
        /// Coerces a value loaded from a <see cref="DataRow"/> column into a
        /// <see cref="Guid"/>. SQLite (and the legacy <c>System.Data.SQLite</c>)
        /// stores GUID values as TEXT and surfaces them as strings; other
        /// providers return native <see cref="Guid"/> instances. This helper
        /// hides that distinction so repository callers never need to
        /// branch on the underlying provider.
        /// </summary>
        private static Guid CoerceToGuid(object value)
        {
            return TryCoerceToGuid(value)
                ?? throw new InvalidOperationException(
                    $"Cannot coerce value of type '{value?.GetType().FullName ?? "null"}' into Guid.");
        }

        private static Guid? TryCoerceToGuid(object? value)
        {
            return value switch
            {
                Guid g => g,
                string s when Guid.TryParse(s, out var parsed) => parsed,
                _ => null,
            };
        }

        // AND-combines the row-id predicate with an optional record-scope filter.
        private static FilterNode CombineWithScope(FilterNode baseFilter, FilterNode? scopeFilter)
            => scopeFilter == null ? baseFilter : FilterGroup.All(baseFilter, scopeFilter);
    }
}
