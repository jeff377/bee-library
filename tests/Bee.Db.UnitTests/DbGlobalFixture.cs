using Bee.Base;
using Bee.Db.Schema;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Extends <see cref="GlobalFixture"/> with database-specific prerequisites:
    /// verifies connectivity and ensures table schemas are created before any test runs.
    /// </summary>
    public class DbGlobalFixture : GlobalFixture
    {
        public DbGlobalFixture() : base()
        {
            var connStr = Environment.GetEnvironmentVariable("BEE_TEST_DB_CONNSTR");
            if (string.IsNullOrEmpty(connStr)) return;

            // 前置動作一：測試資料庫連線
            VerifyConnection();
            // 前置動作二：確認資料庫結構（不存在時自動建立）
            EnsureSchema();
            // 插入最小必要的種子資料
            EnsureSeedData();
        }

        /// <summary>
        /// Verifies that the database connection can be opened successfully.
        /// Throws if the connection cannot be established.
        /// </summary>
        private static void VerifyConnection()
        {
            using var conn = DbFunc.CreateConnection("common");
            conn.Open();
            Console.WriteLine($"DbGlobalFixture: connection verified (State={conn.State})");
        }

        /// <summary>
        /// Uses <see cref="TableSchemaBuilder"/> to compare and create (or upgrade)
        /// each required table based on the TableSchema definition files.
        /// </summary>
        private static void EnsureSchema()
        {
            var builder = new TableSchemaBuilder("common");

            bool created = builder.Execute("common", "st_user");
            Console.WriteLine($"DbGlobalFixture: st_user schema — {(created ? "created/upgraded" : "up-to-date")}");

            created = builder.Execute("common", "st_session");
            Console.WriteLine($"DbGlobalFixture: st_session schema — {(created ? "created/upgraded" : "up-to-date")}");
        }

        /// <summary>
        /// Inserts the minimum seed data required by the tests.
        /// Does nothing if the data already exists.
        /// </summary>
        private static void EnsureSeedData()
        {
            var dbAccess = new DbAccess("common");

            var check = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user WHERE sys_id = {0}", "001");
            var result = dbAccess.Execute(check);

            if (BaseFunc.CInt(result.Scalar!) == 0)
            {
                var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                    "INSERT INTO st_user (sys_rowid, sys_id, sys_name, password, email, note, sys_insert_time) " +
                    "VALUES (NEWID(), {0}, {1}, '', '', '', GETDATE())",
                    "001", "測試管理員");
                dbAccess.Execute(insert);
                Console.WriteLine("DbGlobalFixture: seed user '001' inserted");
            }
            else
            {
                Console.WriteLine("DbGlobalFixture: seed user '001' already exists");
            }
        }
    }
}
