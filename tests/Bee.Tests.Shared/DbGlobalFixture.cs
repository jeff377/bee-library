using Bee.Base;
using Bee.Db;
using Bee.Db.Schema;
using Bee.Definition.Database;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Extends <see cref="GlobalFixture"/> with database-specific prerequisites:
    /// for each <see cref="DatabaseType"/> whose connection string env var is set, verify
    /// connectivity, ensure required schemas exist, and insert seed data. Failure on any
    /// individual database is logged and that database is skipped — it never aborts the
    /// fixture so tests targeting other databases can still run.
    /// </summary>
    public class DbGlobalFixture : GlobalFixture
    {
        public DbGlobalFixture() : base()
        {
            EnsureDatabase(DatabaseType.SQLServer);
            EnsureDatabase(DatabaseType.PostgreSQL);
            EnsureDatabase(DatabaseType.SQLite);
            // 未來新增 MySQL / Oracle 在此擴增。
        }

        /// <summary>
        /// Verifies connection, schema and seed data for the given database type. Skips the
        /// database silently when its connection string env var is not set; logs a warning
        /// and skips the rest when connection or schema setup fails.
        /// </summary>
        private static void EnsureDatabase(DatabaseType dbType)
        {
            var envVar = TestDbConventions.GetConnectionStringEnvVar(dbType);
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar))) return;

            var databaseId = TestDbConventions.GetDatabaseId(dbType);
            try
            {
                VerifyConnection(databaseId);
                EnsureSchema(databaseId);
                EnsureSeedData(dbType, databaseId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DbGlobalFixture: {dbType} setup skipped — {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies that the database connection can be opened successfully.
        /// </summary>
        private static void VerifyConnection(string databaseId)
        {
            using var conn = DbFunc.CreateConnection(databaseId);
            conn.Open();
            Console.WriteLine($"DbGlobalFixture: {databaseId} connection verified (State={conn.State})");
        }

        /// <summary>
        /// Uses <see cref="TableSchemaBuilder"/> to compare and create (or upgrade) each
        /// required table based on the TableSchema definition files. The schema definitions
        /// are dialect-agnostic; the dialect factory routes DDL generation per database type.
        /// </summary>
        private static void EnsureSchema(string databaseId)
        {
            var builder = new TableSchemaBuilder(databaseId);

            bool created = builder.Execute("common", "st_user");
            Console.WriteLine($"DbGlobalFixture: {databaseId} st_user schema — {(created ? "created/upgraded" : "up-to-date")}");

            created = builder.Execute("common", "st_session");
            Console.WriteLine($"DbGlobalFixture: {databaseId} st_session schema — {(created ? "created/upgraded" : "up-to-date")}");
        }

        /// <summary>
        /// Inserts the minimum seed data required by the tests. Uses dialect-specific
        /// scalar expressions (UUID generation, current timestamp) so the same seed runs
        /// against any registered database.
        /// </summary>
        private static void EnsureSeedData(DatabaseType dbType, string databaseId)
        {
            var dbAccess = new DbAccess(databaseId);

            var check = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001");
            var result = dbAccess.Execute(check);

            if (BaseFunc.CInt(result.Scalar!) == 0)
            {
                var (uuid, now) = GetSeedExpressions(dbType);
                var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                    $"INSERT INTO st_user (sys_rowid, sys_id, sys_name, password, email, note, sys_insert_time) " +
                    $"VALUES ({uuid}, {{0}}, {{1}}, '', '', '', {now})",
                    "001", "測試管理員");
                dbAccess.Execute(insert);
                Console.WriteLine($"DbGlobalFixture: {databaseId} seed user '001' inserted");
            }
            else
            {
                Console.WriteLine($"DbGlobalFixture: {databaseId} seed user '001' already exists");
            }
        }

        /// <summary>
        /// Returns the dialect-specific (uuid, timestamp) scalar expressions for seed inserts.
        /// </summary>
        private static (string Uuid, string Now) GetSeedExpressions(DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.SQLServer:
                    return ("NEWID()", "GETDATE()");
                case DatabaseType.PostgreSQL:
                    return ("gen_random_uuid()", "CURRENT_TIMESTAMP");
                case DatabaseType.SQLite:
                    // SQLite has no native UUID generator; hex(randomblob(16)) is unique enough
                    // for seed data even though it isn't a v4 UUID.
                    return ("hex(randomblob(16))", "CURRENT_TIMESTAMP");
                case DatabaseType.MySQL:
                    // UUID() returns a 36-char string; CURRENT_TIMESTAMP(6) matches the
                    // DATETIME(6) microsecond precision used in MySQL CREATE TABLE output.
                    return ("UUID()", "CURRENT_TIMESTAMP(6)");
                default:
                    // NOTE: when adding a new DatabaseType, add a case here as well — otherwise
                    // DbGlobalFixture will throw at fixture init time once a connection string
                    // for the new DB is provided.
                    throw new NotSupportedException($"Seed expressions are not defined for {dbType}.");
            }
        }
    }
}
