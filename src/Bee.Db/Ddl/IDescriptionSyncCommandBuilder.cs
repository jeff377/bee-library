using Bee.Db.Schema;

namespace Bee.Db.Ddl
{
    /// <summary>
    /// Provider-specific builder that persists table and column descriptions (SQL Server
    /// <c>MS_Description</c> extended properties, <c>COMMENT ON</c> on Oracle / PostgreSQL,
    /// the inline <c>COMMENT</c> clause on MySQL) for an in-place upgrade.
    /// </summary>
    /// <remarks>
    /// The CREATE and rebuild paths emit descriptions as part of their own DDL, so this builder
    /// exists solely for the ALTER path. It is given the whole diff rather than a change list
    /// because what has to be emitted is dialect-specific: dialects that carry the description
    /// inside the column definition (MySQL) must not re-emit one the ALTER already applied, while
    /// dialects that store it out-of-band (Oracle / PostgreSQL / SQL Server) additionally have to
    /// cover the columns this plan is about to add — the comparer cannot report drift for a column
    /// that does not exist in the database yet.
    /// </remarks>
    public interface IDescriptionSyncCommandBuilder
    {
        /// <summary>
        /// Generates the statements that align the database's stored descriptions with the definition.
        /// Returns an empty list when there is nothing to apply.
        /// </summary>
        /// <param name="diff">The schema diff being planned.</param>
        IReadOnlyList<string> GetStatements(TableSchemaDiff diff);
    }
}
