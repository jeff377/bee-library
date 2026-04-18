using Bee.Definition.Filters;
using Bee.Definition;
using System;

using Bee.Db;

namespace Bee.Db.Query
{
    /// <summary>
    /// Builds the SQL WHERE clause.
    /// </summary>
    public sealed class WhereBuilder : IWhereBuilder
    {
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// Initializes a new instance of <see cref="WhereBuilder"/>.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        public WhereBuilder(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        /// <summary>
        /// Builds the WHERE clause from a structured filter node tree.
        /// </summary>
        /// <param name="root">The root filter node (may be a group or a single condition).</param>
        /// <param name="selectContext">The field source mappings and table JOIN relationships for the query.</param>
        /// <param name="includeWhereKeyword">Whether to prepend the "WHERE " keyword to the result.</param>
        public WhereBuildResult Build(FilterNode? root, SelectContext? selectContext = null, bool includeWhereKeyword = true)
        {
            if (root == null) { return new WhereBuildResult(); }

            var filter = (selectContext != null)
                               ? RemapFilterNodeFields(root, selectContext)
                               : root;
            var prefix = DbFunc.GetParameterPrefix(_databaseType);
            var parameters = new DefaultParameterCollector(prefix);
            var core = InternalWhereBuilder.BuildNode(filter, parameters);
            var sql = includeWhereKeyword && !string.IsNullOrEmpty(core) ? "WHERE " + core : core;

            return new WhereBuildResult
            {
                WhereClause = sql,
                Parameters = parameters.GetAll()
            };
        }


        /// <summary>
        /// Remaps field names in the filter node to the SQL-qualified format required by the query (prefixed with table alias).
        /// </summary>
        /// <param name="node">The filter node to remap.</param>
        /// <param name="selectContext">The field source mappings and table JOIN relationships for the query.</param>
        /// <returns>The remapped filter node.</returns>
        private FilterNode RemapFilterNodeFields(FilterNode node, SelectContext selectContext)
        {
            if (node.Kind == FilterNodeKind.Condition)
            {
                var cond = (FilterCondition)node;
                var mapping = selectContext.FieldMappings.GetOrDefault(cond.FieldName);
                string fieldExpr;
                if (mapping != null)
                {
                    fieldExpr = $"{mapping.SourceAlias}.{QuoteIdentifier(mapping.SourceField)}";
                }
                else
                {
                    // Field belongs to the main table; default alias is A
                    fieldExpr = $"A.{QuoteIdentifier(cond.FieldName)}";
                }
                return new FilterCondition(fieldExpr, cond.Operator, cond.Value!);
            }
            else if (node.Kind == FilterNodeKind.Group)
            {
                var group = (FilterGroup)node;
                var newGroup = new FilterGroup(group.Operator);
                foreach (var child in group.Nodes)
                    newGroup.Nodes.Add(RemapFilterNodeFields(child, selectContext));
                return newGroup;
            }
            else
            {
                return node;
            }
        }

        private string QuoteIdentifier(string identifier)
        {
            return DbFunc.QuoteIdentifier(_databaseType, identifier);
        }
    }
}
