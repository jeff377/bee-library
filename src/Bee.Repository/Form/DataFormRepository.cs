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
using Bee.Repository.Abstractions.Form;

namespace Bee.Repository.Form
{
    /// <summary>
    /// Repository implementation for data forms; executes FormSchema-driven SELECT
    /// statements against the master table via the dialect-specific
    /// <c>IFormCommandBuilder</c>.
    /// </summary>
    public class DataFormRepository : RepositoryBase, IDataFormRepository
    {
        private readonly FormSchema _schema;

        /// <summary>
        /// Initializes a new <see cref="DataFormRepository"/> for the supplied progId, routed to the
        /// database its form schema's category resolves to.
        /// </summary>
        /// <param name="ctx">The shared repository context.</param>
        /// <param name="accessToken">The current request's access token; required when the schema's category resolves to a company database.</param>
        /// <param name="progId">The program identifier (also the master table name).</param>
        /// <remarks>
        /// WARNING: A subclass adding its own dependencies must not add a second <c>string</c> or
        /// <c>Guid</c> parameter. The factory builds it with <c>ActivatorUtilities</c>, which matches
        /// the supplied arguments to parameters by type: a second parameter of a type already
        /// supplied leaves nothing to bind it to and the container is asked for a <c>string</c>,
        /// which fails at construction. Interface-typed dependencies are resolved from DI and are
        /// the intended way to extend this.
        /// </remarks>
        public DataFormRepository(IRepositoryContext ctx, Guid accessToken, string progId)
            : this(ctx, accessToken, progId, Factories.RepositoryFactory.LoadSchema(
                (ctx ?? throw new ArgumentNullException(nameof(ctx))).DefineAccess, progId))
        {
        }

        private DataFormRepository(IRepositoryContext ctx, Guid accessToken, string progId, FormSchema schema)
            : base(ctx, accessToken, progId, Factories.RepositoryFactory.ParseCategoryId(schema.CategoryId))
        {
            _schema = schema;
        }

        /// <summary>
        /// Initializes a new <see cref="DataFormRepository"/> against an explicit schema and
        /// database, skipping both the definition lookup and the router.
        /// </summary>
        /// <param name="ctx">The shared repository context.</param>
        /// <param name="progId">The program identifier (also the master table name).</param>
        /// <param name="schema">The form schema to drive SELECT construction with.</param>
        /// <param name="databaseId">The database identifier used for connection and dialect resolution.</param>
        /// <remarks>
        /// For tests, and for callers holding a schema that is not (or not yet) in the define store.
        /// A separate constructor rather than optional parameters, so the two construction paths are
        /// distinguishable at the call site.
        /// </remarks>
        public DataFormRepository(IRepositoryContext ctx, string progId, FormSchema schema, string databaseId)
            : base(ctx, progId, databaseId)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

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

            var connInfo = Context.ConnectionManager.GetConnectionInfo(DatabaseId);
            var builder = DbDialectRegistry.Get(connInfo.DatabaseType)
                .CreateFormCommandBuilder(_schema, Context.DefineAccess);
            var dbAccess = Context.DbAccessFactory.Create(DatabaseId);

            if (paging == null)
            {
                var spec = builder.BuildSelect(ProgId, resolvedSelectFields, filter, sortFields);
                return new DataFormListResult { Table = MarkFromSchema(dbAccess.Execute(spec).Table, _schema.MasterTable) };
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
            var table = MarkFromSchema(dbAccess.Execute(pagedSpec).Table, _schema.MasterTable)!;

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
        public DataSet GetNewData(string timeZoneId = "")
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
            // Schema-driven non-null seeding (fresh sys_rowid + type defaults), then the
            // FormSchema field defaults (e.g. a status of "Draft") which take precedence.
            FormRowDefaults.Apply(masterTable, masterRow, null, timeZoneId);
            ApplyMasterDefaults(masterRow, masterTable);
            masterDataTable.Rows.Add(masterRow);

            foreach (var detail in EnumerateDetailTables())
                dataSet.Tables.Add(BuildEmptyDataTable(detail));

            return dataSet;
        }

        /// <inheritdoc/>
        public DataSet? GetData(Guid rowId, FilterNode? scopeFilter = null)
        {
            var connInfo = Context.ConnectionManager.GetConnectionInfo(DatabaseId);
            var builder = DbDialectRegistry.Get(connInfo.DatabaseType)
                .CreateFormCommandBuilder(_schema, Context.DefineAccess);
            var dbAccess = Context.DbAccessFactory.Create(DatabaseId);

            // Master row by sys_rowid, AND-combined with the record-scope filter when supplied so
            // an out-of-scope row reads as "not found" (null).
            var masterFilter = CombineWithScope(FilterCondition.Equal(SysFields.RowId, rowId), scopeFilter);
            var masterSpec = builder.BuildSelect(ProgId, string.Empty, masterFilter);
            var masterDataTable = MarkFromSchema(dbAccess.Execute(masterSpec).Table, _schema.MasterTable);
            if (masterDataTable == null || masterDataTable.Rows.Count == 0)
                return null;

            masterDataTable.TableName = ProgId;

            var dataSet = new DataSet(ProgId);
            dataSet.Tables.Add(masterDataTable);

            var masterRowId = CoerceToGuid(masterDataTable.Rows[0][SysFields.RowId]);
            var detailFilter = FilterCondition.Equal(SysFields.MasterRowId, masterRowId);

            foreach (var detail in EnumerateDetailTables())
            {
                var detailTableName = detail.TableName;
                var detailSpec = builder.BuildSelect(detailTableName, string.Empty, detailFilter);
                var detailDataTable = MarkFromSchema(dbAccess.Execute(detailSpec).Table, detail)
                    ?? new DataTable(detailTableName);
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

            var connInfo = Context.ConnectionManager.GetConnectionInfo(DatabaseId);
            var dbType = connInfo.DatabaseType;

            var masterTable = _schema.MasterTable
                ?? throw new InvalidOperationException(
                    $"FormSchema '{ProgId}' has no master table; cannot Save.");

            // Build one DataTableUpdateSpec per changed table, master before details so a new
            // master is inserted before the detail rows that reference it. Each spec drives an
            // ADO.NET DataAdapter: Added rows INSERT, Deleted rows DELETE, and Modified rows
            // UPDATE the full column set — a Modified row whose values are unchanged simply
            // re-writes the same values, so there is no "empty UPDATE" to special-case.
            var specs = new List<DataTableUpdateSpec>();
            var specTableNames = new List<string>();
            foreach (var formTable in EnumerateTablesMasterFirst(masterTable))
            {
                if (!dataSet.Tables.Contains(formTable.TableName)) { continue; }
                var dataTable = dataSet.Tables[formTable.TableName]!;
                using var changes = dataTable.GetChanges();
                if (changes is null) { continue; }   // nothing pending for this table

                var tableSchema = formTable.GenerateDbTable();
                RemoveProtectedFields(tableSchema);
                var spec = new Bee.Db.Dml.TableSchemaCommandBuilder(dbType, tableSchema).BuildUpdateSpec(dataTable);
                specs.Add(spec);
                specTableNames.Add(formTable.TableName);
            }

            // No pending changes anywhere is a no-op (zero rows affected), not an error.
            var affected = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (specs.Count > 0)
            {
                var dbAccess = Context.DbAccessFactory.Create(DatabaseId);
                var counts = dbAccess.UpdateDataTables(specs);
                for (int i = 0; i < specs.Count; i++)
                    affected[specTableNames[i]] = counts[i];
            }

            // Re-fetch the saved master so server-generated columns surface
            // back to the caller.
            var masterRowId = ExtractMasterRowId(dataSet, masterTable.TableName);
            DataSet? refreshed = masterRowId.HasValue ? GetData(masterRowId.Value) : null;

            return (refreshed, affected);
        }

        /// <inheritdoc/>
        public bool ExistsInScope(Guid rowId, FilterNode? scopeFilter)
        {
            var connInfo = Context.ConnectionManager.GetConnectionInfo(DatabaseId);
            var builder = DbDialectRegistry.Get(connInfo.DatabaseType)
                .CreateFormCommandBuilder(_schema, Context.DefineAccess);
            var dbAccess = Context.DbAccessFactory.Create(DatabaseId);

            var filter = CombineWithScope(FilterCondition.Equal(SysFields.RowId, rowId), scopeFilter);
            var spec = builder.BuildSelect(ProgId, SysFields.RowId, filter);
            var table = dbAccess.Execute(spec).Table;
            return table != null && table.Rows.Count > 0;
        }

        /// <inheritdoc/>
        public int Delete(Guid rowId, FilterNode? scopeFilter = null)
        {
            var connInfo = Context.ConnectionManager.GetConnectionInfo(DatabaseId);
            var builder = DbDialectRegistry.Get(connInfo.DatabaseType)
                .CreateFormCommandBuilder(_schema, Context.DefineAccess);

            // Record-scope gate: when a scope filter is supplied, confirm the master row is in scope
            // before touching anything. Out of scope → delete nothing (no cascade), report zero.
            if (scopeFilter != null && !ExistsInScope(rowId, scopeFilter))
                return 0;

            var batch = new DbBatchSpec { UseTransaction = true };

            // Cascade delete details first (FK on sys_master_rowid), then master.
            var detailFilter = FilterCondition.Equal(SysFields.MasterRowId, rowId);
            foreach (var detail in EnumerateDetailTables())
                batch.Commands.Add(builder.BuildDelete(detail.TableName, detailFilter));

            var masterFilter = FilterCondition.Equal(SysFields.RowId, rowId);
            batch.Commands.Add(builder.BuildDelete(ProgId, masterFilter));

            var dbAccess = Context.DbAccessFactory.Create(DatabaseId);
            var result = dbAccess.ExecuteBatch(batch);

            // The master DELETE is the last command; its RowsAffected drives
            // the caller-visible count.
            var lastIndex = result.Results.Count - 1;
            return lastIndex >= 0 ? result.Results[lastIndex].RowsAffected : 0;
        }

        /// <summary>
        /// Drops any <see cref="ProtectedFields"/> column from the schema that drives the INSERT and
        /// UPDATE commands, so a form declaring one cannot write it.
        /// </summary>
        /// <remarks>
        /// WARNING: this is a privilege boundary, not a tidy-up. It is applied here rather than in
        /// `TableSchemaCommandBuilder` on purpose — the builder is a general DML tool with no
        /// business of knowing which columns the framework reserves, whereas this path is exactly
        /// the one a deployment's own FormSchema reaches. The generated schema is a fresh object per
        /// call (`FormTable.GenerateDbTable`), so removing from it never touches cached definitions.
        /// </remarks>
        private static void RemoveProtectedFields(TableSchema tableSchema)
        {
            var fields = tableSchema.Fields;
            if (fields == null) { return; }

            for (int i = fields.Count - 1; i >= 0; i--)
            {
                if (ProtectedFields.IsProtected(tableSchema.TableName, fields[i].FieldName))
                    fields.RemoveAt(i);
            }
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

        /// <summary>
        /// Enumerates the form's tables master-first, then each detail. Save applies them in this
        /// order so a newly inserted master row exists before the detail rows that reference it.
        /// </summary>
        private IEnumerable<FormTable> EnumerateTablesMasterFirst(FormTable masterTable)
        {
            yield return masterTable;
            foreach (var detail in EnumerateDetailTables())
                yield return detail;
        }

        /// <summary>
        /// Replays the schema's declared field types over a table read from the database.
        /// </summary>
        /// <param name="table">The table returned by the query; null passes through.</param>
        /// <param name="formTable">The form table describing the query shape.</param>
        /// <remarks>
        /// A provider reports a calendar-day column as `DateTime`, indistinguishable from an instant.
        /// Marking here keeps a table read from SQL describing itself the same way as one built from
        /// the schema by <see cref="BuildEmptyDataTable"/>, whose `AddColumn` calls mark as they build.
        /// </remarks>
        private static DataTable? MarkFromSchema(DataTable? table, FormTable? formTable)
        {
            if (table != null && formTable != null)
                formTable.ApplyFieldDbTypes(table);
            return table;
        }

        private static DataTable BuildEmptyDataTable(FormTable formTable)
        {
            var dataTable = new DataTable(formTable.TableName);
            if (formTable.Fields == null)
                return dataTable;

            foreach (FormField field in formTable.Fields)
            {
                // The skeleton mirrors the GetData SELECT shape: persistent columns
                // plus relation display fields (`ref_*`), which the client lookup
                // write-back fills locally — without the column the write is silently
                // dropped and the picked value never shows on a new record. Virtual
                // (calculated) fields stay excluded; the command builders filter by
                // `FieldType.DbField`, so the extra columns never reach the SQL.
                if (field.Type == FieldType.VirtualField)
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
