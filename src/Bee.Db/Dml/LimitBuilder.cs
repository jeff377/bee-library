using System.Globalization;
using Bee.Definition.Database;

namespace Bee.Db.Dml
{
    /// <summary>
    /// Builds the SQL paging clause for the underlying dialect. Inlines
    /// <c>skip</c> / <c>take</c> as integer literals because:
    /// <list type="bullet">
    /// <item>The values are framework-controlled <see cref="int"/> (no injection risk).</item>
    /// <item>Several dialects do not accept parameters in LIMIT-style clauses.</item>
    /// <item>Inlining avoids parameter-name collisions with the WHERE clause.</item>
    /// </list>
    /// This is consistent with the codebase's "framework structural elements inline,
    /// user-supplied values parameterised" philosophy (table names, aliases and quoted
    /// identifiers are all inlined the same way). Do not refactor to parameterised
    /// binds without first verifying every dialect accepts placeholders in LIMIT/FETCH.
    /// </summary>
    public sealed class LimitBuilder : ILimitBuilder
    {
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// Initializes a new instance of <see cref="LimitBuilder"/>.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        public LimitBuilder(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        /// <inheritdoc/>
        public string Build(int? skip, int? take)
        {
            if (skip == null && take == null) { return string.Empty; }
            if (skip is < 0) { throw new ArgumentOutOfRangeException(nameof(skip)); }
            if (take is < 0) { throw new ArgumentOutOfRangeException(nameof(take)); }

            return _databaseType switch
            {
                DatabaseType.SQLServer or DatabaseType.Oracle
                    => BuildOffsetFetch(skip, take),
                DatabaseType.PostgreSQL or DatabaseType.SQLite
                    => BuildLimitOffset(skip, take),
                DatabaseType.MySQL
                    => BuildLimitOffsetMySql(skip, take),
                _ => throw new NotSupportedException($"Paging is not supported for database type: {_databaseType}"),
            };
        }

        // SQL Server / Oracle 12c+: OFFSET ... ROWS [FETCH NEXT ... ROWS ONLY]
        // Standard SQL:2008 syntax, requires ORDER BY when used.
        private static string BuildOffsetFetch(int? skip, int? take)
        {
            int s = skip ?? 0;
            return take.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", s, take.Value)
                : string.Format(CultureInfo.InvariantCulture, "OFFSET {0} ROWS", s);
        }

        // PostgreSQL / SQLite: LIMIT and OFFSET may appear independently.
        private static string BuildLimitOffset(int? skip, int? take)
        {
            if (skip == null) { return string.Format(CultureInfo.InvariantCulture, "LIMIT {0}", take); }
            if (take == null) { return string.Format(CultureInfo.InvariantCulture, "OFFSET {0}", skip); }
            return string.Format(CultureInfo.InvariantCulture, "LIMIT {0} OFFSET {1}", take, skip);
        }

        // MySQL: OFFSET cannot appear alone; when skip is specified without take we
        // emit LIMIT 18446744073709551615 (UINT64_MAX) as the documented sentinel
        // meaning "all remaining rows" — see MySQL Reference Manual, SELECT syntax.
        private static string BuildLimitOffsetMySql(int? skip, int? take)
        {
            if (skip == null) { return string.Format(CultureInfo.InvariantCulture, "LIMIT {0}", take); }
            if (take == null) { return string.Format(CultureInfo.InvariantCulture, "LIMIT 18446744073709551615 OFFSET {0}", skip); }
            return string.Format(CultureInfo.InvariantCulture, "LIMIT {0} OFFSET {1}", take, skip);
        }
    }
}
