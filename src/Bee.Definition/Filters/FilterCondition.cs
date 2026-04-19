using MessagePack;

namespace Bee.Definition.Filters
{
    /// <summary>
    /// A single-field filter condition (e.g., Name LIKE '%Lee%', Age &gt; 18).
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public sealed class FilterCondition : FilterNode
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FilterCondition"/>.
        /// </summary>
        public FilterCondition() { }

        /// <summary>
        /// Initializes a new instance of <see cref="FilterCondition"/>.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="operator">The comparison operator.</param>
        /// <param name="value">The primary value.</param>
        /// <param name="secondValue">The second value (used for Between conditions).</param>
        public FilterCondition(string fieldName, ComparisonOperator @operator, object value, object? secondValue = null)
        {
            FieldName = fieldName;
            Operator = @operator;
            Value = value;
            SecondValue = secondValue;
        }

        /// <summary>
        /// Gets the node kind.
        /// </summary>
        public override FilterNodeKind Kind { get { return FilterNodeKind.Condition; } }

        /// <summary>
        /// Gets or sets the field name.
        /// </summary>
        [Key(100)]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the comparison operator.
        /// </summary>
        [Key(101)]
        public ComparisonOperator Operator { get; set; }

        /// <summary>
        /// Gets or sets the primary value (used for Equal, Like, &gt;, etc.).
        /// </summary>
        [Key(102)]
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets the second value (used for Between conditions).
        /// </summary>
        [Key(103)]
        public object? SecondValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore this condition when the value is null.
        /// </summary>
        [Key(104)]
        public bool IgnoreIfNull { get; set; }

        /// <summary>
        /// Creates an Equal condition.
        /// </summary>
        public static FilterCondition Equal(string fieldName, object value, bool ignoreIfNull = false)
        {
            return new FilterCondition { FieldName = fieldName, Operator = ComparisonOperator.Equal, Value = value, IgnoreIfNull = ignoreIfNull };
        }

        /// <summary>
        /// Creates a NotEqual condition.
        /// </summary>
        public static FilterCondition NotEqual(string fieldName, object value, bool ignoreIfNull = false)
        {
            return new FilterCondition { FieldName = fieldName, Operator = ComparisonOperator.NotEqual, Value = value, IgnoreIfNull = ignoreIfNull };
        }

        /// <summary>
        /// Creates a Contains (LIKE '%value%') condition.
        /// </summary>
        public static FilterCondition Contains(string fieldName, string keyword)
        {
            return new FilterCondition { FieldName = fieldName, Operator = ComparisonOperator.Contains, Value = keyword };
        }

        /// <summary>
        /// Creates a StartsWith condition (equivalent to SQL LIKE 'value%').
        /// </summary>
        public static FilterCondition StartsWith(string fieldName, string prefix)
        {
            return new FilterCondition { FieldName = fieldName, Operator = ComparisonOperator.StartsWith, Value = prefix };
        }

        /// <summary>
        /// Creates an EndsWith condition (equivalent to SQL LIKE '%value').
        /// </summary>
        public static FilterCondition EndsWith(string fieldName, string suffix)
        {
            return new FilterCondition { FieldName = fieldName, Operator = ComparisonOperator.EndsWith, Value = suffix };
        }

        /// <summary>
        /// Creates a Between condition.
        /// </summary>
        public static FilterCondition Between(string fieldName, object from, object to, bool ignoreIfNull = false)
        {
            return new FilterCondition { FieldName = fieldName, Operator = ComparisonOperator.Between, Value = from, SecondValue = to, IgnoreIfNull = ignoreIfNull };
        }

        /// <summary>
        /// Creates an In condition.
        /// </summary>
        public static FilterCondition In(string field, IEnumerable<object> values)
        {
            return new FilterCondition { FieldName = field, Operator = ComparisonOperator.In, Value = values };
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            string op;
            switch (Operator)
            {
                case ComparisonOperator.Equal:
                    op = "=";
                    break;
                case ComparisonOperator.NotEqual:
                    op = "<>";
                    break;
                case ComparisonOperator.GreaterThan:
                    op = ">";
                    break;
                case ComparisonOperator.GreaterThanOrEqual:
                    op = ">=";
                    break;
                case ComparisonOperator.LessThan:
                    op = "<";
                    break;
                case ComparisonOperator.LessThanOrEqual:
                    op = "<=";
                    break;
                case ComparisonOperator.Like:
                    op = "LIKE";
                    break;
                case ComparisonOperator.In:
                    op = "IN";
                    break;
                case ComparisonOperator.Between:
                    op = "BETWEEN";
                    break;
                case ComparisonOperator.StartsWith:
                case ComparisonOperator.EndsWith:
                case ComparisonOperator.Contains:
                    op = "LIKE";
                    break;
                default:
                    op = Operator.ToString();
                    break;
            }

            string? valueStr;
            if (Operator == ComparisonOperator.In && Value is IEnumerable<object> values)
            {
                valueStr = $"({string.Join(", ", values)})";
            }
            else if (Value is string s)
            {
                valueStr = $"'{s}'";
            }
            else
            {
                valueStr = Value?.ToString();
            }

            if (Operator == ComparisonOperator.Between)
            {
                string? fromStr = Value is string fs ? $"'{fs}'" : Value?.ToString();
                string? toStr = SecondValue is string ts ? $"'{ts}'" : SecondValue?.ToString();
                return $"{FieldName} {op} {fromStr} AND {toStr}";
            }
            if (Operator == ComparisonOperator.StartsWith)
                valueStr = $"'{Value}%'";
            else if (Operator == ComparisonOperator.EndsWith)
                valueStr = $"'%{Value}'";
            else if (Operator == ComparisonOperator.Contains)
                valueStr = $"'%{Value}%'";

            return $"{FieldName} {op} {valueStr}";
        }
    }
}
