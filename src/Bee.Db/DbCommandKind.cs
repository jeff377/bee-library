namespace Bee.Db
{
    /// <summary>
    /// Specifies the execution kind of a database command.
    /// </summary>
    public enum DbCommandKind
    {
        /// <summary>
        /// Executes a command that does not return a result set; returns the number of rows affected.
        /// </summary>
        NonQuery,
        /// <summary>
        /// Executes a command and returns a single scalar value (e.g., COUNT(*)).
        /// </summary>
        Scalar,
        /// <summary>
        /// Executes a command and returns a full DataTable result set.
        /// </summary>
        DataTable
    }
}
