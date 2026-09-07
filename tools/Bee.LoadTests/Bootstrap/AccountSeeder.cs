using System.Globalization;
using Bee.Base.Security;
using Bee.Db;
using Bee.LoadTests.Configuration;

namespace Bee.LoadTests.Bootstrap
{
    /// <summary>
    /// Seeds the accounts, company and access grants a run signs in with.
    /// </summary>
    /// <remarks>
    /// Sign-in goes through the framework's own implementation, which reads three tables:
    /// <c>st_user</c> for the account, <c>st_company</c> for the company a session enters, and
    /// <c>st_user_company</c> for the grant between them. All three live in the common category.
    /// </remarks>
    public static class AccountSeeder
    {
        /// <summary>
        /// Ensures the company, the account pool and their grants exist.
        /// </summary>
        /// <param name="dbAccess">Database access for the common category.</param>
        /// <param name="auth">Authentication configuration.</param>
        /// <param name="companyDatabaseId">The database id the company's data lives in.</param>
        /// <returns>The number of accounts inserted; zero when the pool already existed.</returns>
        public static int EnsureAccounts(DbAccess dbAccess, AuthOptions auth, string companyDatabaseId)
        {
            ArgumentNullException.ThrowIfNull(dbAccess);
            ArgumentNullException.ThrowIfNull(auth);
            ArgumentException.ThrowIfNullOrWhiteSpace(companyDatabaseId);

            var companyRowId = EnsureCompany(dbAccess, auth, companyDatabaseId);

            var inserted = 0;
            for (int index = 0; index < auth.UserPoolSize; index++)
            {
                var userId = auth.UserIdPrefix + index.ToString(CultureInfo.InvariantCulture);
                var userRowId = ResolveRowId(dbAccess, "st_user", userId);

                if (userRowId == Guid.Empty)
                {
                    userRowId = InsertUser(dbAccess, userId, auth.Password);
                    inserted++;
                }

                EnsureGrant(dbAccess, userRowId, companyRowId);
            }
            return inserted;
        }

        private static Guid EnsureCompany(DbAccess dbAccess, AuthOptions auth, string companyDatabaseId)
        {
            var existing = ResolveRowId(dbAccess, "st_company", auth.CompanyId);
            if (existing != Guid.Empty) { return existing; }

            var rowId = Guid.NewGuid();
            const string sql =
                "INSERT INTO st_company (sys_rowid, sys_id, sys_name, company_database_id, customize_id, " +
                "number_formats_xml, default_currency, cash_rounding_xml, allowed_currencies_xml, enabled) " +
                "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})";

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql,
                rowId,
                auth.CompanyId,
                "Load test company",
                companyDatabaseId,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                true));

            return rowId;
        }

        private static Guid InsertUser(DbAccess dbAccess, string userId, string password)
        {
            var rowId = Guid.NewGuid();
            const string sql =
                "INSERT INTO st_user (sys_rowid, sys_id, sys_name, password, email, note, " +
                "time_zone, culture, deployment_admin) " +
                "VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8})";

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql,
                rowId,
                userId,
                userId,
                // Stored as a hash, the same way any account is: sign-in runs the framework's own
                // verification, so a plain-text value here would simply fail to authenticate.
                PasswordHasher.HashPassword(password),
                string.Empty,
                string.Empty,
                "UTC",
                "en-US",
                false));

            return rowId;
        }

        private static void EnsureGrant(DbAccess dbAccess, Guid userRowId, Guid companyRowId)
        {
            var countSpec = new DbCommandSpec(DbCommandKind.Scalar,
                "SELECT COUNT(*) FROM st_user_company WHERE user_rowid = {0} AND company_rowid = {1}",
                userRowId, companyRowId);

            if (Convert.ToInt32(dbAccess.Execute(countSpec).Scalar, CultureInfo.InvariantCulture) > 0)
            {
                return;
            }

            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                "INSERT INTO st_user_company (sys_rowid, user_rowid, company_rowid) VALUES ({0}, {1}, {2})",
                Guid.NewGuid(), userRowId, companyRowId));
        }

        private static Guid ResolveRowId(DbAccess dbAccess, string tableName, string sysId)
        {
            var spec = new DbCommandSpec(DbCommandKind.Scalar,
                $"SELECT sys_rowid FROM {tableName} WHERE sys_id = {{0}}", sysId);
            var value = dbAccess.Execute(spec).Scalar;
            return value is Guid guid ? guid : Guid.Empty;
        }
    }
}
