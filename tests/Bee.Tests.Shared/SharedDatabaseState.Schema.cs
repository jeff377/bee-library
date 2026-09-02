using System.Data.Common;
using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Db.Schema;
using Bee.Definition.Database;
using Bee.Definition.Storage;

namespace Bee.Tests.Shared
{
    /// <content>
    /// Per-database setup: physical database creation, the connection check that decides whether
    /// a database participates at all, and the table build — plus the step-failure bookkeeping
    /// that keeps one broken table from costing the rest of the setup.
    /// </content>
    public static partial class SharedDatabaseState
    {
        private static void EnsureDatabase(DatabaseType dbType, IDefineAccess access, IDbConnectionManager connectionManager)
        {
            var envVar = TestDbConventions.GetConnectionStringEnvVar(dbType);
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar))) return;

            var commonDatabaseId = TestDbConventions.GetDatabaseId(dbType);
            try
            {
                EnsurePhysicalDatabasesExist(dbType, access);
                VerifyConnection(commonDatabaseId, connectionManager);
            }
            catch (DbException ex)
            {
                // Reachability is the one failure worth tolerating: a connection string pointing
                // at a container that is not running must skip this database rather than fail the
                // run, which is what keeps one absent engine from taking the other four with it.
                // `DbException` is the ADO.NET base every provider derives from, so this needs no
                // per-provider matching.
                Console.WriteLine($"SharedDatabaseState: {dbType} unreachable — setup skipped ({ex.GetType().Name}: {ex.Message})");
                return;
            }

            // The engine has answered, so every remaining step runs even when an earlier one
            // failed: one table that will not build must not cost this database its seed data.
            // Aborting the whole sequence is how a single CREATE TABLE conflict used to turn
            // into "User not found." across unrelated tests further down the run.
            var failures = new List<SetupStepFailure>();
            EnsureSchema(dbType, access, connectionManager, failures);
            RunStep(failures, "seed data", () => EnsureSeedData(dbType, commonDatabaseId, connectionManager));
            // Business-table seed lives in the company database. Idempotent (per-table
            // empty check); only runs when the company category is registered.
            RunStep(failures, "Northwind seed", () => NorthwindTestSeed.Seed(dbType, access, connectionManager));

            ReportSetupFailures(dbType, failures);
            VerifySetupUsable(dbType, commonDatabaseId, connectionManager, failures);
        }

        private sealed record SetupStepFailure(string Step, DbException Exception);

        // Records instead of propagating: the caller decides what a given failure costs, and it
        // cannot decide that from inside the first step that happens to break.
        private static void RunStep(List<SetupStepFailure> failures, string step, Action action)
        {
            try
            {
                action();
            }
            catch (DbException ex)
            {
                failures.Add(new SetupStepFailure(step, ex));
            }
        }

        private static void ReportSetupFailures(DatabaseType dbType, List<SetupStepFailure> failures)
        {
            if (failures.Count == 0) return;

            // Loud on purpose. The old single "setup skipped" line read like an intentional skip,
            // so a broken setup and a healthy one looked alike in the log.
            Console.WriteLine($"SharedDatabaseState: !!! {dbType} setup completed with {failures.Count} failed step(s):");
            foreach (var failure in failures)
            {
                Console.WriteLine($"SharedDatabaseState: !!!   [{failure.Step}] {failure.Exception.GetType().Name}: {failure.Exception.Message}");
                Console.WriteLine(failure.Exception.ToString());
            }
        }

        // The suite's own precondition, checked where the cause is still in hand: a database the
        // fixture connected to but left without its seed user cannot run a single test that needs
        // one, and every symptom of that surfaces far away from here. Individual step failures are
        // tolerated (a log table that will not upgrade costs only its own tests); this one is not.
        private static void VerifySetupUsable(
            DatabaseType dbType, string commonDatabaseId, IDbConnectionManager connectionManager, List<SetupStepFailure> failures)
        {
            var dbAccess = new DbAccess(commonDatabaseId, connectionManager);
            try
            {
                var seedUser = LookupRowId(
                    dbType, dbAccess, dbType.QuoteIdentifier("st_user"),
                    dbType.QuoteIdentifier("sys_rowid"), dbType.QuoteIdentifier("sys_id"), "001");
                if (seedUser != Guid.Empty) return;
            }
            catch (DbException ex)
            {
                // The probe itself failing (no such table, for one) is the same verdict with a
                // less readable message, so it joins the report rather than escaping raw.
                failures.Add(new SetupStepFailure("seed user probe", ex));
            }

            var detail = failures.Count == 0
                ? "no step reported a failure."
                : string.Join(" | ", failures.Select(f => $"[{f.Step}] {f.Exception.GetType().Name}: {f.Exception.Message}"));
            throw new InvalidOperationException(
                $"SharedDatabaseState: {dbType} is reachable but has no seed user '001' in {commonDatabaseId} — {detail}");
        }

        // Auto-creates the per-category physical database (e.g. company on SQL Server,
        // PostgreSQL, MySQL) when missing. Best-effort: connects to the engine's admin
        // database and runs a CREATE DATABASE statement; on permission failure the
        // exception is logged and the schema-build step will raise a clearer error.
        // Oracle and SQLite are skipped (Oracle uses single-schema mode; SQLite in-memory
        // DBs come into being on first connection).
        private static void EnsurePhysicalDatabasesExist(DatabaseType dbType, IDefineAccess access)
        {
            if (dbType == DatabaseType.Oracle || dbType == DatabaseType.SQLite) return;

            var categorySettings = access.GetDbCategorySettings();
            if (categorySettings?.Categories == null) return;

            var serverId = TestDbConventions.GetServerId(dbType);
            var dbSettings = access.GetDatabaseSettings();
            if (dbSettings.Servers == null || !dbSettings.Servers.Contains(serverId)) return;
            var serverConnStr = dbSettings.Servers[serverId].ConnectionString;

            var adminDbName = GetAdminDatabaseName(dbType);
            var adminConnStr = StringUtilities.Replace(serverConnStr, "{@DbName}", adminDbName);
            var providerFactory = DbProviderRegistry.Get(dbType);

            foreach (var category in categorySettings.Categories)
            {
                if (category.Tables == null || category.Tables.Count == 0) continue;
                var dbName = category.Id;
                try
                {
                    using var conn = providerFactory.CreateConnection()!;
                    conn.ConnectionString = adminConnStr;
                    conn.Open();
                    CreateDatabaseIfMissing(dbType, conn, dbName);
                    Console.WriteLine($"SharedDatabaseState: {dbType} physical database '{dbName}' ensured");
                }
                catch (DbException ex)
                {
                    // Best-effort by design: an unreachable engine is reported again — and acted
                    // on — by the connection check that follows, and a missing CREATE DATABASE
                    // grant surfaces with a clearer message from the schema step.
                    Console.WriteLine($"SharedDatabaseState: {dbType} CREATE DATABASE '{dbName}' failed (may need manual setup + grant) — {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        private static string GetAdminDatabaseName(DatabaseType dbType) => dbType switch
        {
            DatabaseType.SQLServer => "master",
            DatabaseType.PostgreSQL => "postgres",
            // MySQL: an empty initial database is valid; pick the always-present "mysql"
            // schema to avoid driver-specific edge cases when DB=empty.
            DatabaseType.MySQL => "mysql",
            _ => string.Empty
        };

        private static void CreateDatabaseIfMissing(DatabaseType dbType, System.Data.Common.DbConnection conn, string dbName)
        {
            switch (dbType)
            {
                case DatabaseType.SQLServer:
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"IF DB_ID(N'{dbName}') IS NULL CREATE DATABASE [{dbName}]";
                    cmd.ExecuteNonQuery();
                    break;
                }
                case DatabaseType.PostgreSQL:
                {
                    // PG does not support IF NOT EXISTS on CREATE DATABASE and CREATE DATABASE
                    // cannot run inside a transaction block; do an explicit existence probe first.
                    using (var probe = conn.CreateCommand())
                    {
                        probe.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{dbName}'";
                        if (probe.ExecuteScalar() != null) return;
                    }
                    using var create = conn.CreateCommand();
                    create.CommandText = $"CREATE DATABASE \"{dbName}\"";
                    create.ExecuteNonQuery();
                    break;
                }
                case DatabaseType.MySQL:
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{dbName}`";
                    cmd.ExecuteNonQuery();
                    break;
                }
            }
        }

        private static void VerifyConnection(string databaseId, IDbConnectionManager connectionManager)
        {
            using var conn = connectionManager.CreateConnection(databaseId);
            conn.Open();
            Console.WriteLine($"SharedDatabaseState: {databaseId} connection verified (State={conn.State})");
        }

        private static void EnsureSchema(
            DatabaseType dbType, IDefineAccess access, IDbConnectionManager connectionManager, List<SetupStepFailure> failures)
        {
            var settings = access.GetDbCategorySettings();
            if (settings?.Categories == null) return;

            foreach (var category in settings.Categories)
            {
                if (category.Tables == null || category.Tables.Count == 0) continue;

                var databaseId = TestDbConventions.GetDatabaseId(dbType, category.Id);
                var builder = new TableSchemaBuilder(databaseId, access, connectionManager);
                var schemaProvider = DbDialectRegistry.Get(dbType).CreateTableSchemaProvider(databaseId, connectionManager);

                foreach (var table in category.Tables)
                {
                    var tableName = table.TableName;
                    RunStep(failures, $"{databaseId}.{tableName} schema",
                        () => EnsureTable(builder, schemaProvider, databaseId, category.Id, tableName));
                }
            }
        }

        // Building a table is a read-then-create pair inside the framework builder: it reads the
        // actual schema, finds nothing, and plans a CREATE. Two test processes arriving together
        // both plan one, and the loser's statement fails on an object that now exists — SQL Server
        // says "There is already an object named 'st_user'", every other provider words it its own
        // way. The verdict is therefore taken from the database rather than from the error text or
        // code: the table was absent before the attempt and is present after it, so the winner
        // built it and there is nothing left to do. Any other failure propagates to the caller,
        // which records it against this table without giving up on the rest of the database.
        private static void EnsureTable(
            TableSchemaBuilder builder,
            ITableSchemaProvider schemaProvider,
            string databaseId,
            string categoryId,
            string tableName)
        {
            bool wasMissing = schemaProvider.GetTableSchema(tableName) is null;
            try
            {
                bool created = builder.Execute(categoryId, tableName);
                Console.WriteLine($"SharedDatabaseState: {databaseId} {tableName} schema — {(created ? "created/upgraded" : "up-to-date")}");
            }
            catch (DbException ex)
            {
                if (!wasMissing || schemaProvider.GetTableSchema(tableName) is null) throw;
                Console.WriteLine($"SharedDatabaseState: {databaseId} {tableName} schema — created by a concurrent test process ({ex.GetType().Name} ignored)");
            }
        }
    }
}
