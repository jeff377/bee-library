using Bee.Definition;
using Bee.Definition.Filters;
using Bee.Definition.Forms;

namespace Bee.Db.Sql
{
    /// <summary>
    /// Builds DELETE command specifications from a form schema.
    /// </summary>
    public class DeleteCommandBuilder
    {
        private readonly FormSchema _formSchema;
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// Initializes a new instance of <see cref="DeleteCommandBuilder"/>.
        /// </summary>
        /// <param name="formSchema">The form schema definition.</param>
        /// <param name="databaseType">The database type.</param>
        public DeleteCommandBuilder(FormSchema formSchema, DatabaseType databaseType)
        {
            _formSchema = formSchema ?? throw new ArgumentNullException(nameof(formSchema));
            _databaseType = databaseType;
        }

        /// <summary>
        /// Builds a DELETE <see cref="DbCommandSpec"/> for the specified table using the supplied filter.
        /// The filter may only reference columns of the target table itself (no JOIN, no RelationField).
        /// </summary>
        /// <param name="tableName">The form table name.</param>
        /// <param name="filter">The filter that becomes the WHERE clause; must not be null.</param>
        public DbCommandSpec Build(string tableName, FilterNode filter)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("tableName cannot be null or whitespace.", nameof(tableName));
            if (filter == null)
                throw new ArgumentNullException(nameof(filter), "DELETE without a filter is not supported.");

            if (!_formSchema.Tables!.Contains(tableName))
                throw new InvalidOperationException($"Cannot find the specified table: {tableName}");
            var formTable = _formSchema.Tables[tableName];

            var quotedFilter = QuoteAndValidateFields(filter, formTable);

            var whereBuilder = new WhereBuilder(_databaseType);
            var whereResult = whereBuilder.Build(quotedFilter, selectContext: null, includeWhereKeyword: true);
            if (string.IsNullOrWhiteSpace(whereResult.WhereClause))
                throw new InvalidOperationException(
                    $"Filter for table '{tableName}' produced an empty WHERE clause; refusing to build an unbounded DELETE.");

            string dbTableName = !string.IsNullOrWhiteSpace(formTable.DbTableName)
                ? formTable.DbTableName
                : formTable.TableName;
            string quotedTable = DbFunc.QuoteIdentifier(_databaseType, dbTableName);

            string sql = $"DELETE FROM {quotedTable} {whereResult.WhereClause}";
            return new DbCommandSpec(DbCommandKind.NonQuery, sql, whereResult.Parameters);
        }

        /// <summary>
        /// Walks the filter tree, validates that each referenced field belongs to the target table
        /// and is not a RelationField, and returns a copy with field names replaced by quoted identifiers.
        /// </summary>
        private FilterNode QuoteAndValidateFields(FilterNode node, FormTable formTable)
        {
            if (node.Kind == FilterNodeKind.Condition)
            {
                var cond = (FilterCondition)node;
                if (string.IsNullOrEmpty(cond.FieldName))
                    throw new InvalidOperationException("Filter condition has empty FieldName.");

                if (!formTable.Fields!.Contains(cond.FieldName))
                    throw new NotSupportedException(
                        $"DELETE filter references unknown field '{cond.FieldName}' on table '{formTable.TableName}'. "
                        + "Cross-table or RelationField conditions are not supported.");

                var field = formTable.Fields[cond.FieldName];
                if (field.Type != FieldType.DbField)
                    throw new NotSupportedException(
                        $"DELETE filter cannot reference {field.Type} field '{cond.FieldName}'. "
                        + "Only physical columns of the target table are supported.");

                string quoted = DbFunc.QuoteIdentifier(_databaseType, cond.FieldName);
                return new FilterCondition
                {
                    FieldName = quoted,
                    Operator = cond.Operator,
                    Value = cond.Value,
                    SecondValue = cond.SecondValue,
                    IgnoreIfNull = cond.IgnoreIfNull,
                };
            }

            if (node.Kind == FilterNodeKind.Group)
            {
                var group = (FilterGroup)node;
                var newGroup = new FilterGroup(group.Operator);
                foreach (var child in group.Nodes)
                    newGroup.Nodes.Add(QuoteAndValidateFields(child, formTable));
                return newGroup;
            }

            return node;
        }
    }
}
