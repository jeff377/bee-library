namespace Bee.Definition.Filters
{
    /// <summary>
    /// Comparison operator.
    /// Represents the various types of comparison operations available in queries or conditions.
    /// </summary>
    public enum ComparisonOperator
    {
        /// <summary>
        /// Equal to, corresponding to SQL "=".
        /// </summary>
        Equal = 0,
        /// <summary>
        /// Not equal to, corresponding to SQL "&lt;&gt;" or "!=".
        /// </summary>
        NotEqual = 1,
        /// <summary>
        /// Greater than, corresponding to SQL "&gt;".
        /// </summary>
        GreaterThan = 2,
        /// <summary>
        /// Greater than or equal to, corresponding to SQL "&gt;=".
        /// </summary>
        GreaterThanOrEqual = 3,
        /// <summary>
        /// Less than, corresponding to SQL "&lt;".
        /// </summary>
        LessThan = 4,
        /// <summary>
        /// Less than or equal to, corresponding to SQL "&lt;=".
        /// </summary>
        LessThanOrEqual = 5,
        /// <summary>
        /// Pattern matching, corresponding to SQL "LIKE" (the caller must supply appropriate wildcard characters).
        /// </summary>
        Like = 6,
        /// <summary>
        /// Set membership, corresponding to SQL "IN ( ... )".
        /// </summary>
        In = 7,
        /// <summary>
        /// Range check, corresponding to SQL "BETWEEN ... AND ...".
        /// </summary>
        Between = 8,
        /// <summary>
        /// Starts-with match, equivalent to SQL "LIKE 'value%'".
        /// </summary>
        StartsWith = 9,
        /// <summary>
        /// Ends-with match, equivalent to SQL "LIKE '%value'".
        /// </summary>
        EndsWith = 10,
        /// <summary>
        /// Contains match, equivalent to SQL "LIKE '%value%'".
        /// </summary>
        Contains = 11
    }
}
