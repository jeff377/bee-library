namespace Bee.Definition.Filters
{
    /// <summary>
    /// Represents a logical operator used to combine groups or query/condition expressions.
    /// </summary>
    /// <remarks>
    /// Describes how multiple conditions are combined (e.g., AND/OR in query conditions).
    /// </remarks>
    public enum LogicalOperator
    {
        /// <summary>
        /// Logical AND.
        /// </summary>
        And = 0,
        /// <summary>
        /// Logical OR.
        /// </summary>
        Or = 1
    }
}
