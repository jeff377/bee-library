using Bee.Definition.Filters;
using Bee.Definition.Forms;
using Bee.Definition;
using Bee.Db.Query;

namespace Bee.Db.Providers
{
    /// <summary>
    /// Builds SELECT command specifications from a form schema.
    /// </summary>
    public class SelectCommandBuilder
    {
        private readonly FormSchema _formDefine;
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// Initializes a new instance of <see cref="SelectCommandBuilder"/>.
        /// </summary>
        /// <param name="formDefine">The form schema definition.</param>
        /// <param name="databaseType">The database type.</param>
        public SelectCommandBuilder(FormSchema formDefine, DatabaseType databaseType)
        {
            _formDefine = formDefine;
            _databaseType = databaseType;
        }

        /// <summary>
        /// Builds a SELECT <see cref="DbCommandSpec"/>.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="selectFields">A comma-separated list of field names to retrieve; empty string retrieves all fields.</param>
        /// <param name="filter">The filter condition.</param>
        /// <param name="sortFields">The sort field collection.</param>
        public DbCommandSpec Build(string tableName, string selectFields, FilterNode? filter = null, SortFieldCollection? sortFields = null)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("tableName cannot be null or whitespace.", nameof(tableName));

            var formTable = _formDefine.Tables![tableName];
            if (formTable == null)
                throw new InvalidOperationException($"Cannot find the specified table: {tableName}");

            var selectContext = GetSelectContext(formTable, selectFields, filter, sortFields);

            var sqlParts = new List<string>
            {
                BuildSelectClause(formTable, selectFields, selectContext),
                BuildFromClause(formTable, selectContext.Joins)
            };

            var (whereClause, parameters) = BuildWhereClause(filter, selectContext);
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sqlParts.Add(whereClause);
            }

            var orderByClause = BuildOrderByClause(sortFields, selectContext);
            if (!string.IsNullOrWhiteSpace(orderByClause))
            {
                sqlParts.Add(orderByClause);
            }

            string sql = string.Join(Environment.NewLine, sqlParts);
            return new DbCommandSpec(DbCommandKind.DataTable, sql, parameters);
        }

        /// <summary>
        /// Gets the field source mappings and JOIN relationships required for a SELECT query.
        /// </summary>
        /// <param name="formTable">The form table definition.</param>
        /// <param name="selectFields">A comma-separated list of field names; empty string retrieves all fields.</param>
        /// <param name="filter">The filter condition.</param>
        /// <param name="sortFields">The sort field collection.</param>
        private static SelectContext GetSelectContext(FormTable formTable, string selectFields, FilterNode? filter, SortFieldCollection? sortFields)
        {
            var usedFieldNames = GetUsedFieldNames(formTable, selectFields, filter, sortFields);
            var builder = new SelectContextBuilder(formTable, usedFieldNames);
            return builder.Build();
        }

        /// <summary>
        /// Builds the SELECT clause.
        /// </summary>
        /// <param name="formTable">The form table definition.</param>
        /// <param name="selectFields">A comma-separated list of field names; empty string retrieves all fields.</param>
        /// <param name="selectContext">The field source mappings and JOIN relationships.</param>
        private string BuildSelectClause(FormTable formTable, string selectFields, SelectContext selectContext)
        {
            var builder = new SelectBuilder(_databaseType);
            return builder.Build(formTable, selectFields, selectContext);
        }

        /// <summary>
        /// Builds the FROM clause.
        /// </summary>
        /// <param name="formTable">The form table definition.</param>
        /// <param name="joins">The table JOIN relationship collection.</param>
        /// <returns>The FROM clause string.</returns>
        private string BuildFromClause(FormTable formTable, TableJoinCollection joins)
        {
            string mainTableName = !string.IsNullOrWhiteSpace(formTable.DbTableName) ? formTable.DbTableName : formTable.TableName;
            var builder = new FromBuilder(_databaseType);
            return builder.Build(mainTableName, joins);
        }

        /// <summary>
        /// Builds the WHERE clause.
        /// </summary>
        /// <param name="filter">The filter condition.</param>
        /// <param name="selectContext">The field source mappings and JOIN relationships for the query.</param>
        /// <returns>A tuple containing the WHERE clause string and the parameter dictionary.</returns>
        private (string WhereClause, IDictionary<string, object>? Parameters) BuildWhereClause(FilterNode? filter, SelectContext selectContext)
        {
            var whereBuilder = new WhereBuilder(_databaseType);
            var whereResult = whereBuilder.Build(filter, selectContext, true);
            return (whereResult.WhereClause, whereResult.Parameters);
        }

        /// <summary>
        /// Builds the ORDER BY clause.
        /// </summary>
        /// <param name="sortFields">The sort field collection.</param>
        /// <param name="selectContext">The field source mappings and JOIN relationships for the query.</param>
        /// <returns>The ORDER BY clause string, or null if no sort fields are specified.</returns>
        private string BuildOrderByClause(SortFieldCollection? sortFields, SelectContext selectContext)
        {
            var sortBuilder = new SortBuilder(_databaseType);
            return sortBuilder.Build(sortFields, selectContext);
        }

        /// <summary>
        /// Returns the unique set of field names used across selectFields, filter, and sortFields.
        /// </summary>
        /// <param name="formTable">The form table definition.</param>
        /// <param name="selectFields">A comma-separated list of field names; empty string means all fields.</param>
        /// <param name="filter">The filter condition.</param>
        /// <param name="sortFields">The sort field collection.</param>
        /// <returns>A deduplicated set of field names.</returns>
        private static HashSet<string> GetUsedFieldNames(FormTable formTable, string selectFields, FilterNode? filter, SortFieldCollection? sortFields)
        {
            var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // selectFields
            if (string.IsNullOrWhiteSpace(selectFields))
            {
                foreach (var field in formTable.Fields!)
                {
                    fieldNames.Add(field.FieldName);
                }
            }
            else
            {
                var selectFieldArr = selectFields.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var field in selectFieldArr)
                {
                    fieldNames.Add(field.Trim());
                }
            }

            // filter
            CollectFilterFields(filter, fieldNames);

            // sortFields
            if (sortFields != null)
            {
                foreach (var sortField in sortFields.Where(s => !string.IsNullOrWhiteSpace(s.FieldName)))
                {
                    fieldNames.Add(sortField.FieldName);
                }
            }

            return fieldNames;
        }

        /// <summary>
        /// Recursively collects all field names used within a <see cref="FilterNode"/>.
        /// </summary>
        /// <param name="node">The filter condition node.</param>
        /// <param name="fieldNames">The set to add field names to.</param>
        private static void CollectFilterFields(FilterNode? node, HashSet<string> fieldNames)
        {
            if (node == null) return;
            if (node.Kind == FilterNodeKind.Condition)
            {
                var cond = (FilterCondition)node;
                if (!string.IsNullOrWhiteSpace(cond.FieldName))
                    fieldNames.Add(cond.FieldName);
            }
            else if (node.Kind == FilterNodeKind.Group)
            {
                var group = (FilterGroup)node;
                foreach (var child in group.Nodes)
                    CollectFilterFields(child, fieldNames);
            }
        }
    }
}