using Bee.Define;
using System;
using System.Collections.Generic;

namespace Bee.Db
{
    /// <summary>
    /// 內部共用的節點轉 SQL 工具。
    /// </summary>
    internal static class InternalWhereBuilder
    {
        /// <summary>
        /// 將過濾節點轉換為 SQL 條件字串。
        /// </summary>
        /// <param name="node">要轉換的過濾節點（可能是 FilterGroup 或 FilterCondition）。</param>
        /// <param name="parameters">參數收集器，用於收集 SQL 參數值。</param>
        /// <returns>產生的 SQL 條件字串；如果節點為空或所有子節點為空，則返回空字串。</returns>
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

            var field = c.FieldName;

            if (c.Value == null)
            {
                if (c.IgnoreIfNull) return string.Empty;

                if (c.Operator == ComparisonOperator.Equal) return field + " IS NULL";
                if (c.Operator == ComparisonOperator.NotEqual) return field + " IS NOT NULL";

                throw new InvalidOperationException("Value is null but operator is not supported with null.");
            }

            switch (c.Operator)
            {
                case ComparisonOperator.Equal: return field + " = " + parameters.Add(c.Value);
                case ComparisonOperator.NotEqual: return field + " <> " + parameters.Add(c.Value);
                case ComparisonOperator.GreaterThan: return field + " > " + parameters.Add(c.Value);
                case ComparisonOperator.GreaterThanOrEqual: return field + " >= " + parameters.Add(c.Value);
                case ComparisonOperator.LessThan: return field + " < " + parameters.Add(c.Value);
                case ComparisonOperator.LessThanOrEqual: return field + " <= " + parameters.Add(c.Value);
                case ComparisonOperator.Like: return field + " LIKE " + parameters.Add(c.Value);
                case ComparisonOperator.Contains: return field + " LIKE " + parameters.Add("%" + c.Value + "%");
                case ComparisonOperator.StartsWith: return field + " LIKE " + parameters.Add(c.Value + "%");
                case ComparisonOperator.EndsWith: return field + " LIKE " + parameters.Add("%" + c.Value);
                case ComparisonOperator.In:
                    {
                        var enumerable = c.Value as System.Collections.Generic.IEnumerable<object>;
                        if (enumerable == null)
                            throw new InvalidOperationException("IN operator requires an enumerable value.");
                        var list = new List<string>();
                        foreach (var item in enumerable)
                            list.Add(parameters.Add(item));
                        if (list.Count == 0) return "1 = 0";
                        return field + " IN (" + string.Join(", ", list.ToArray()) + ")";
                    }
                case ComparisonOperator.Between:
                    {
                        if (c.SecondValue == null)
                        {
                            if (c.IgnoreIfNull) return string.Empty;
                            throw new InvalidOperationException("Between operator requires two values.");
                        }
                        var p1 = parameters.Add(c.Value);
                        var p2 = parameters.Add(c.SecondValue);
                        return field + " BETWEEN " + p1 + " AND " + p2;
                    }
                default:
                    throw new NotSupportedException("Unsupported comparison operator.");
            }
        }
    }
}
