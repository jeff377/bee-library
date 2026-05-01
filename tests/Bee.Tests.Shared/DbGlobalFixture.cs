using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
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
        // 同 GlobalFixture：single-host 模式下多個 collection 並行創建 fixture instance 會
        // race（重複 schema build、重複 seed insert）。lock + once flag 確保整個 process 內
        // 只執行一次 DB 初始化。
        private static readonly object _dbInitLock = new();
        private static bool _dbInitialized;

        public DbGlobalFixture() : base()
        {
            lock (_dbInitLock)
            {
                if (_dbInitialized) return;
                EnsureDatabase(DatabaseType.SQLServer);
                EnsureDatabase(DatabaseType.PostgreSQL);
                EnsureDatabase(DatabaseType.SQLite);
                EnsureDatabase(DatabaseType.MySQL);
                EnsureDatabase(DatabaseType.Oracle);
                _dbInitialized = true;
            }
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
            using var conn = DbConnectionManager.CreateConnection(databaseId);
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

            // 表名與欄位名一律 dialect-quote：Oracle 對 unquoted 識別符自動轉 UPPERCASE，
            // 而 framework CREATE TABLE 是 quoted lowercase 形式，unquoted SELECT/INSERT
            // 會找不到 ST_USER。對其他 DB（quoted 後仍為原大小寫）行為一致。
            string tbl = dbType.QuoteIdentifier("st_user");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colPwd = dbType.QuoteIdentifier("password");
            string colEmail = dbType.QuoteIdentifier("email");
            string colNote = dbType.QuoteIdentifier("note");
            string colInsTime = dbType.QuoteIdentifier("sys_insert_time");

            var check = new DbCommandSpec(DbCommandKind.Scalar,
                $"SELECT COUNT(*) FROM {tbl} WHERE {colId} = {{0}}", "001");
            var result = dbAccess.Execute(check);

            if (BaseFunc.CInt(result.Scalar!) == 0)
            {
                var (uuid, now) = GetSeedExpressions(dbType);
                // password/email/note 使用單空白字元而非空字串：Oracle 將 empty string 視為
                // NULL，會違反 NOT NULL constraint；其他 DB 仍視為一字元字串。如此 5 DB 行為一致。
                var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                    $"INSERT INTO {tbl} ({colRowId}, {colId}, {colName}, {colPwd}, {colEmail}, {colNote}, {colInsTime}) " +
                    $"VALUES ({uuid}, {{0}}, {{1}}, ' ', ' ', ' ', {now})",
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
                case DatabaseType.Oracle:
                    // SYS_GUID() returns a 16-byte RAW; SYSTIMESTAMP carries time-zone info
                    // and aligns with the TIMESTAMP(6) precision used in Oracle CREATE TABLE output.
                    return ("SYS_GUID()", "SYSTIMESTAMP");
                default:
                    // NOTE: when adding a new DatabaseType, add a case here as well — otherwise
                    // DbGlobalFixture will throw at fixture init time once a connection string
                    // for the new DB is provided.
                    throw new NotSupportedException($"Seed expressions are not defined for {dbType}.");
            }
        }
    }
}
