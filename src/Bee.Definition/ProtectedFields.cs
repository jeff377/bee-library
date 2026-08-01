namespace Bee.Definition
{
    /// <summary>
    /// Columns the framework refuses to write through the FormSchema-driven data path, whatever a
    /// deployment's own definitions declare.
    /// </summary>
    /// <remarks>
    /// WARNING: entries here guard privilege, not data shape. The FormSchema path writes whichever
    /// columns a form declares, so a deployment that builds a maintenance form over one of these
    /// tables would otherwise hand its ordinary users a way to grant themselves the column's
    /// privilege. Each protected column has exactly one legitimate write path — a dedicated,
    /// separately gated operation — and this list is what keeps that "exactly one" true.
    /// <para>
    /// Reads are unaffected: a form may display a protected column, it simply cannot store one.
    /// </para>
    /// </remarks>
    public static class ProtectedFields
    {
        /// <summary>
        /// The deployment administrator flag on <c>st_user</c>. Written only by
        /// <c>SystemBO.SetDeploymentAdmin</c>, which is restricted to local calls.
        /// </summary>
        public const string DeploymentAdmin = "deployment_admin";

        private static readonly HashSet<string> s_protected = new(StringComparer.OrdinalIgnoreCase)
        {
            $"st_user.{DeploymentAdmin}",
        };

        /// <summary>
        /// Determines whether a column may not be written through the FormSchema-driven data path.
        /// </summary>
        /// <param name="tableName">The database table name.</param>
        /// <param name="fieldName">The column name.</param>
        /// <returns><c>true</c> when the column is protected; otherwise, <c>false</c>.</returns>
        public static bool IsProtected(string tableName, string fieldName)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(fieldName)) { return false; }
            return s_protected.Contains($"{tableName}.{fieldName}");
        }
    }
}
