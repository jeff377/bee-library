using Bee.Definition.Storage;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Extends <see cref="GlobalFixture"/> with database-specific prerequisites: for each
    /// <see cref="Bee.Definition.Database.DatabaseType"/> whose connection-string env var
    /// is set, <see cref="SharedDatabaseState.EnsureSchemaAndSeed"/> verifies connectivity,
    /// creates / upgrades the <c>st_user</c>/<c>st_session</c> schemas, and inserts the
    /// seed user. Failure on any individual DB is logged and skipped — it never aborts the
    /// fixture so tests targeting other databases can still run.
    /// </summary>
    public class DbGlobalFixture : GlobalFixture
    {
        public DbGlobalFixture() : base()
        {
            SharedDatabaseState.EnsureSchemaAndSeed(BeeTestServices.GetRequiredService<IDefineAccess>());
        }
    }
}
