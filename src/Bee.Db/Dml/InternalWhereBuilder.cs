using Bee.Definition.Filters;
using Bee.Definition;

namespace Bee.Db.Dml
{
    /// <summary>
    /// Internal shared utility for converting filter nodes to SQL condition strings.
    /// </summary>
    internal static class InternalWhereBuilder
    {
        private const string LikeOperator = " LIKE ";


        /// <summary>
        /// Converts a filter node to a SQL condition string.
        /// </summary>
        /// <param name="node">The filter node to convert (may be a <see cref="FilterGroup"/> or <see cref="FilterCondition"/>).</param>
        /// <param name="parameters">The parameter collector used to gather SQL parameter values.</param>
        /// <returns>The generated SQL condition string; returns an empty string if the node is empty or all child nodes are empty.</returns>
        public static string BuildNode(FilterNode node, IParameterCollector parameters)
        {
            var group = node as FilterGroup;
            if (group != null)
            {
                var parts = new List<string>();
                for (int i = 0; i < group.Nodes.Count; i++)
                {
                    var child = group.Nodes[i];
                    var s = BuildNode(child, parameters);
                    if (!string.IsNullOrEmpty(s)) parts.Add(s);
                }
                if (parts.Count == 0) return string.Empty;
                var joiner = group.Operator == LogicalOperator.And ? " AND " : " OR ";
                return "(" + string.Join(joiner, parts.ToArray()) + ")";
            }

            var cond = node as FilterCondition;
            if (cond != null) return BuildCondition(cond, parameters);

            throw new NotSupportedException("Unknown filter node type.");
        }

        private static string BuildCondition(FilterCondition c, IParameterCollector parameters)
        {
            if (string.IsNullOrEmpty(c.FieldName))
                throw new InvalidOperationException("Field name cannot be null or empty.");

            if (c.Value == null)
                return BuildNullCondition(c);

            return c.Operator switch
            {
                ComparisonOperator.Equal => c.FieldName + " = " + parameters.Add(c.Value),
                ComparisonOperator.NotEqual => c.FieldName + " <> " + parameters.Add(c.Value),
                ComparisonOperator.GreaterThan => c.FieldName + " > " + parameters.Add(c.Value),
                ComparisonOperator.GreaterThanOrEqual => c.FieldName + " >= " + parameters.Add(c.Value),
                ComparisonOperator.LessThan => c.FieldName + " < " + parameters.Add(c.Value),
                ComparisonOperator.LessThanOrEqual => c.FieldName + " <= " + parameters.Add(c.Value),
                ComparisonOperator.Like => c.FieldName + LikeOperator + parameters.Add(c.Value),
                ComparisonOperator.Contains => c.FieldName + LikeOperator + parameters.Add("%" + c.Value + "%"),
                ComparisonOperator.StartsWith => c.FieldName + LikeOperator + parameters.Add(c.Value + "%"),
                ComparisonOperator.EndsWith => c.FieldName + LikeOperator + parameters.Add("%" + c.Value),
                ComparisonOperator.In => BuildInCondition(c, parameters),
                ComparisonOperator.Between => BuildBetweenCondition(c, parameters),
                _ => throw new NotSupportedException("Unsupported comparison operator."),
            };
        }

        private static string BuildNullCondition(FilterCondition c)
        {
            if (c.IgnoreIfNull) return string.Empty;
            if (c.Operator == ComparisonOperator.Equal) return c.FieldName + " IS NULL";
            if (c.Operator == ComparisonOperator.NotEqual) return c.FieldName + " IS NOT NULL";
            throw new InvalidOperationException("Value is null but operator is not supported with null.");
        }

        private static string BuildInCondition(FilterCondition c, IParameterCollector parameters)
        {
            var enumerable = c.Value as System.Collections.Generic.IEnumerable<object>
                ?? throw new InvalidOperationException("IN operator requires an enumerable value.");
            var list = new List<string>();
            foreach (var item in enumerable)
                list.Add(parameters.Add(item));
            if (list.Count == 0) return "1 = 0";
            return c.FieldName + " IN (" + string.Join(", ", list.ToArray()) + ")";
        }

        private static string BuildBetweenCondition(FilterCondition c, IParameterCollector parameters)
        {
            if (c.SecondValue == null)
            {
                if (c.IgnoreIfNull) return string.Empty;
                throw new InvalidOperationException("Between operator requires two values.");
            }
            var p1 = parameters.Add(c.Value!);
            var p2 = parameters.Add(c.SecondValue);
            return c.FieldName + " BETWEEN " + p1 + " AND " + p2;
        }
    }
}
