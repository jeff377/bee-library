using System.Collections.Generic;

namespace Bee.Db.Query
{
    /// <summary>
    /// Represents the result of building a WHERE clause.
    /// </summary>
    public sealed class WhereBuildResult
    {
        /// <summary>
        /// Gets or sets the WHERE clause string (with or without the "WHERE" keyword).
        /// </summary>
        public string WhereClause { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the named parameters generated for the WHERE clause.
        /// </summary>
        public IDictionary<string, object>? Parameters { get; set; } = null;
    }
}
