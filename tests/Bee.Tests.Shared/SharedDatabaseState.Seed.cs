using System.Data.Common;
using Bee.Base;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;

namespace Bee.Tests.Shared
{
    /// <content>
    /// The seed rows every fixture depends on (user <c>001</c>, company <c>C001</c> and the link
    /// between them) and the dialect-specific expressions they are written with.
    /// </content>
    public static partial class SharedDatabaseState
    {
        private static void EnsureSeedData(DatabaseType dbType, string databaseId, IDbConnectionManager connectionManager)
        {
            var dbAccess = new DbAccess(databaseId, connectionManager);

            var userRowId = EnsureSeedUser(dbType, databaseId, dbAccess);
            var companyRowId = EnsureSeedCompany(dbType, databaseId, dbAccess);
            EnsureSeedUserCompany(dbType, databaseId, dbAccess, userRowId, companyRowId);
        }

        // Each seed row is written as probe-then-insert, so the insert can lose a race with a
        // concurrent test process that committed the same business key in between (the unique
        // `sys_id` index is what makes the loser fail rather than duplicate). The verdict comes
        // from the database, not from a provider-specific unique-violation code: re-probe, and
        // adopt the winner's row id when it is there. An empty probe means the failure was
        // something else and rethrows.
        private static Guid InsertOrAdopt(string databaseId, string description, Func<Guid> insert, Func<Guid> probe)
        {
            try
            {
                return insert();
            }
            catch (DbException ex)
            {
                var adopted = probe();
                if (adopted == Guid.Empty) throw;
                Console.WriteLine($"SharedDatabaseState: {databaseId} {description} — adopted a concurrent insert (rowid={adopted}; {ex.GetType().Name} ignored)");
                return adopted;
            }
        }

        // 表名與欄位名一律 dialect-quote：Oracle 對 unquoted 識別符自動轉 UPPERCASE，
        // 而 framework CREATE TABLE 是 quoted lowercase 形式，unquoted SELECT/INSERT
        // 會找不到 ST_USER。對其他 DB（quoted 後仍為原大小寫）行為一致。
        private static Guid EnsureSeedUser(DatabaseType dbType, string databaseId, DbAccess dbAccess)
        {
            string tbl = dbType.QuoteIdentifier("st_user");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colPwd = dbType.QuoteIdentifier("password");
            string colEmail = dbType.QuoteIdentifier("email");
            string colNote = dbType.QuoteIdentifier("note");
            string colTimeZone = dbType.QuoteIdentifier("time_zone");
            string colCulture = dbType.QuoteIdentifier("culture");
            string colInsTime = dbType.QuoteIdentifier("sys_insert_time");

            var existing = LookupRowId(dbType, dbAccess, tbl, colRowId, colId, "001");
            if (existing != Guid.Empty)
            {
                // Existence alone is not the goal — the seed must converge on a known state. A row
                // inserted before `time_zone` / `culture` existed carries no value, so backfill it.
                // The predicate makes each statement idempotent and safe to race: concurrent test
                // processes either write the same value or find nothing to write. Oracle stores ''
                // as NULL, so the IS NULL arm covers it there and the equality arm covers the other
                // providers.
                var backfill = new DbCommandSpec(DbCommandKind.NonQuery,
                    $"UPDATE {tbl} SET {colTimeZone} = 'Asia/Taipei' " +
                    $"WHERE {colId} = {{0}} AND ({colTimeZone} IS NULL OR {colTimeZone} = '')",
                    "001");
                dbAccess.Execute(backfill);
                var backfillCulture = new DbCommandSpec(DbCommandKind.NonQuery,
                    $"UPDATE {tbl} SET {colCulture} = 'zh-TW' " +
                    $"WHERE {colId} = {{0}} AND ({colCulture} IS NULL OR {colCulture} = '')",
                    "001");
                dbAccess.Execute(backfillCulture);
                Console.WriteLine($"SharedDatabaseState: {databaseId} seed user '001' already exists (rowid={existing})");
                return existing;
            }

            var (_, now) = GetSeedExpressions(dbType);
            var newRowId = Guid.NewGuid();
            // password/email/note 使用單空白字元而非空字串：Oracle 將 empty string 視為
            // NULL，會違反 NOT NULL constraint；其他 DB 仍視為一字元字串。如此 5 DB 行為一致。
            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tbl} ({colRowId}, {colId}, {colName}, {colPwd}, {colEmail}, {colNote}, {colTimeZone}, {colCulture}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, ' ', ' ', ' ', 'Asia/Taipei', 'zh-TW', {now})",
                newRowId, "001", "測試管理員");
            return InsertOrAdopt(databaseId, "seed user '001'",
                () =>
                {
                    dbAccess.Execute(insert);
                    Console.WriteLine($"SharedDatabaseState: {databaseId} seed user '001' inserted (rowid={newRowId})");
                    return newRowId;
                },
                () => LookupRowId(dbType, dbAccess, tbl, colRowId, colId, "001"));
        }

        private static Guid EnsureSeedCompany(DatabaseType dbType, string databaseId, DbAccess dbAccess)
        {
            string tbl = dbType.QuoteIdentifier("st_company");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colId = dbType.QuoteIdentifier("sys_id");
            string colName = dbType.QuoteIdentifier("sys_name");
            string colDbId = dbType.QuoteIdentifier("company_database_id");
            string colNumFmt = dbType.QuoteIdentifier("number_formats_xml");
            string colDefCur = dbType.QuoteIdentifier("default_currency");
            string colCashRnd = dbType.QuoteIdentifier("cash_rounding_xml");
            string colAllowCur = dbType.QuoteIdentifier("allowed_currencies_xml");
            string colEnabled = dbType.QuoteIdentifier("enabled");
            string colInsTime = dbType.QuoteIdentifier("sys_insert_time");

            var existing = LookupRowId(dbType, dbAccess, tbl, colRowId, colId, "C001");
            if (existing != Guid.Empty)
            {
                Console.WriteLine($"SharedDatabaseState: {databaseId} seed company 'C001' already exists (rowid={existing})");
                return existing;
            }

            var (_, now) = GetSeedExpressions(dbType);
            var newRowId = Guid.NewGuid();
            // company_database_id 指向該 company 的資料庫（company-category DatabaseItem.Id），
            // 也就是 permission 表（st_role_grant / st_user_role）實際所在的庫 —— EnterCompany 會用
            // 此值載入角色權限快照。測試環境下即 company_sqlserver / company_postgresql / ...。
            var companyDbId = TestDbConventions.GetDatabaseId(dbType, "company");
            // 各方言 boolean literal：SQL Server/SQLite/MySQL/Oracle 用 1，PG 用 TRUE。
            string enabledLiteral = dbType == DatabaseType.PostgreSQL ? "TRUE" : "1";
            // number_formats_xml / cash_rounding_xml / allowed_currencies_xml are NOT NULL Text columns
            // with no overrides here, and default_currency is a NOT NULL String column. MySQL TEXT
            // columns cannot carry a DEFAULT, so every hand-written INSERT must supply these values
            // explicitly (an empty string) rather than relying on a DB-side default.
            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tbl} ({colRowId}, {colId}, {colName}, {colDbId}, {colNumFmt}, {colDefCur}, {colCashRnd}, {colAllowCur}, {colEnabled}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}}, {{7}}, {enabledLiteral}, {now})",
                newRowId, "C001", "測試公司", companyDbId, string.Empty, string.Empty, string.Empty, string.Empty);
            return InsertOrAdopt(databaseId, "seed company 'C001'",
                () =>
                {
                    dbAccess.Execute(insert);
                    Console.WriteLine($"SharedDatabaseState: {databaseId} seed company 'C001' inserted (rowid={newRowId})");
                    return newRowId;
                },
                () => LookupRowId(dbType, dbAccess, tbl, colRowId, colId, "C001"));
        }

        private static void EnsureSeedUserCompany(
            DatabaseType dbType, string databaseId, DbAccess dbAccess, Guid userRowId, Guid companyRowId)
        {
            string tbl = dbType.QuoteIdentifier("st_user_company");
            string colRowId = dbType.QuoteIdentifier("sys_rowid");
            string colUserRowId = dbType.QuoteIdentifier("user_rowid");
            string colCompanyRowId = dbType.QuoteIdentifier("company_rowid");
            string colInsTime = dbType.QuoteIdentifier("sys_insert_time");

            var check = new DbCommandSpec(DbCommandKind.Scalar,
                $"SELECT COUNT(*) FROM {tbl} WHERE {colUserRowId} = {{0}} AND {colCompanyRowId} = {{1}}",
                userRowId, companyRowId);
            var result = dbAccess.Execute(check);
            if (ValueUtilities.CInt(result.Scalar!) > 0)
            {
                Console.WriteLine($"SharedDatabaseState: {databaseId} seed user-company link already exists");
                return;
            }

            var (_, now) = GetSeedExpressions(dbType);
            var insert = new DbCommandSpec(DbCommandKind.NonQuery,
                $"INSERT INTO {tbl} ({colRowId}, {colUserRowId}, {colCompanyRowId}, {colInsTime}) " +
                $"VALUES ({{0}}, {{1}}, {{2}}, {now})",
                Guid.NewGuid(), userRowId, companyRowId);
            try
            {
                dbAccess.Execute(insert);
                Console.WriteLine($"SharedDatabaseState: {databaseId} seed user-company link inserted ('001' ↔ 'C001')");
            }
            catch (DbException ex)
            {
                // Same probe-then-insert race as the rows above, re-checked the same way.
                var recheck = dbAccess.Execute(check);
                if (ValueUtilities.CInt(recheck.Scalar!) == 0) throw;
                Console.WriteLine($"SharedDatabaseState: {databaseId} seed user-company link — created by a concurrent test process ({ex.GetType().Name} ignored)");
            }
        }

        // SELECT sys_rowid by business key; returns Guid.Empty if not found.
        // Handles Oracle RAW(16) (returned as byte[]) and string-storage (SQLite) alongside native Guid.
        private static Guid LookupRowId(
            DatabaseType dbType, DbAccess dbAccess, string tbl, string colRowId, string colBusinessKey, string businessKey)
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                $"SELECT {colRowId} FROM {tbl} WHERE {colBusinessKey} = {{0}}", businessKey);
            var result = dbAccess.Execute(spec);
            return ToGuid(result.Scalar);
        }

        private static Guid ToGuid(object? value)
        {
            if (value is null || value is DBNull) return Guid.Empty;
            if (value is Guid g) return g;
            if (value is byte[] b && b.Length == 16) return new Guid(b);
            if (value is string s && Guid.TryParse(s, out var parsed)) return parsed;
            return Guid.Empty;
        }

        private static (string Uuid, string Now) GetSeedExpressions(DatabaseType dbType)
        {
            switch (dbType)
            {
                // The timestamp expressions are all the UTC-returning form. Each provider's default
                // "now" reads the server's local clock — except SQLite's, which is already UTC — so
                // seeding with them would put five different bases into columns ADR-032 D1 defines
                // as UTC, and the resulting drift would look like a framework bug rather than seed data.
                case DatabaseType.SQLServer:
                    return ("NEWID()", "GETUTCDATE()");
                case DatabaseType.PostgreSQL:
                    return ("gen_random_uuid()", "(NOW() AT TIME ZONE 'UTC')");
                case DatabaseType.SQLite:
                    // SQLite has no native UUID generator; hex(randomblob(16)) is unique enough
                    // for seed data even though it isn't a v4 UUID. Its CURRENT_TIMESTAMP is UTC.
                    return ("hex(randomblob(16))", "CURRENT_TIMESTAMP");
                case DatabaseType.MySQL:
                    return ("UUID()", "UTC_TIMESTAMP(6)");
                case DatabaseType.Oracle:
                    return ("SYS_GUID()", "SYS_EXTRACT_UTC(SYSTIMESTAMP)");
                default:
                    // NOTE: when adding a new DatabaseType, add a case here as well —
                    // otherwise SharedDatabaseState will throw at fixture init time
                    // once a connection string for the new DB is provided.
                    throw new NotSupportedException($"Seed expressions are not defined for {dbType}.");
            }
        }
    }
}
