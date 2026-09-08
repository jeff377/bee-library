using System.ComponentModel;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// <see cref="DbCommandSpec"/> 佔位符綁定的跨 provider 合約測試：
    /// 佔位符的**書寫順序**與參數集合的順序無關，同一個佔位符也可以在一句 SQL 內出現多次。
    /// </summary>
    /// <remarks>
    /// Oracle.ManagedDataAccess binds by position unless <c>BindByName</c> is set, so before
    /// 4.27.x both shapes bound the wrong value to the wrong column there — silently for
    /// compatible types, and as ORA-00932 when they were not. Sign-in's
    /// <c>UPDATE st_session</c> is written in the first shape, which is why every request that
    /// needed a session failed on Oracle. These run on every supported provider: the point is
    /// that the placeholder API means the same thing on all of them.
    /// </remarks>
    public class ParameterBindingOrderTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public ParameterBindingOrderTests(SharedDbFixture fx) { _fx = fx; }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：佔位符非遞增順序時仍應綁到對應的欄位")]
        public void OutOfOrderPlaceholders_SqlServer() => RunOutOfOrderPlaceholders(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：佔位符非遞增順序時仍應綁到對應的欄位")]
        public void OutOfOrderPlaceholders_PostgreSql() => RunOutOfOrderPlaceholders(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：佔位符非遞增順序時仍應綁到對應的欄位")]
        public void OutOfOrderPlaceholders_MySql() => RunOutOfOrderPlaceholders(DatabaseType.MySQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：佔位符非遞增順序時仍應綁到對應的欄位")]
        public void OutOfOrderPlaceholders_Sqlite() => RunOutOfOrderPlaceholders(DatabaseType.SQLite);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：佔位符非遞增順序時仍應綁到對應的欄位")]
        public void OutOfOrderPlaceholders_Oracle() => RunOutOfOrderPlaceholders(DatabaseType.Oracle);

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server：同一佔位符出現兩次應綁到同一個參數")]
        public void RepeatedPlaceholder_SqlServer() => RunRepeatedPlaceholder(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL：同一佔位符出現兩次應綁到同一個參數")]
        public void RepeatedPlaceholder_PostgreSql() => RunRepeatedPlaceholder(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL：同一佔位符出現兩次應綁到同一個參數")]
        public void RepeatedPlaceholder_MySql() => RunRepeatedPlaceholder(DatabaseType.MySQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite：同一佔位符出現兩次應綁到同一個參數")]
        public void RepeatedPlaceholder_Sqlite() => RunRepeatedPlaceholder(DatabaseType.SQLite);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle：同一佔位符出現兩次應綁到同一個參數")]
        public void RepeatedPlaceholder_Oracle() => RunRepeatedPlaceholder(DatabaseType.Oracle);

        // The production shape this reproduces is SessionRepository.UpdateSession: two columns
        // set, then the key matched, so the key's placeholder is written last while its value is
        // first in the collection. The columns are deliberately of three different types — under
        // positional binding a same-type mismatch would go undetected by the database and only
        // show up as wrong data.
        private void RunOutOfOrderPlaceholders(DatabaseType dbType)
        {
            var dbAccess = _fx.NewDbAccess(TestDbConventions.GetDatabaseId(dbType, "common"));
            var token = Guid.NewGuid();
            try
            {
                InsertSessionRow(dbAccess, token, "initial", DateTime.UtcNow.AddHours(1));

                var newInvalidTime = new DateTime(2031, 3, 4, 5, 6, 7, DateTimeKind.Utc);
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "UPDATE st_session SET session_user_xml={1}, sys_invalid_time={2} WHERE access_token={0}",
                    token, "updated", newInvalidTime));

                var table = dbAccess.Execute(new DbCommandSpec(DbCommandKind.DataTable,
                    "SELECT session_user_xml, sys_invalid_time FROM st_session WHERE access_token={0}",
                    token)).Table;

                Assert.NotNull(table);
                Assert.Single(table!.Rows);
                Assert.Equal("updated", Convert.ToString(table.Rows[0]["session_user_xml"], System.Globalization.CultureInfo.InvariantCulture));
                Assert.Equal(newInvalidTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                    Convert.ToDateTime(table.Rows[0]["sys_invalid_time"], System.Globalization.CultureInfo.InvariantCulture)
                        .ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
            }
            finally
            {
                DeleteSessionRow(dbAccess, token);
            }
        }

        private void RunRepeatedPlaceholder(DatabaseType dbType)
        {
            var dbAccess = _fx.NewDbAccess(TestDbConventions.GetDatabaseId(dbType, "common"));
            var token = Guid.NewGuid();
            try
            {
                InsertSessionRow(dbAccess, token, "initial", DateTime.UtcNow.AddHours(1));

                // {0} twice: one parameter, two bind sites. Positional binding would look for a
                // second parameter to fill the second site.
                var count = dbAccess.Execute(new DbCommandSpec(DbCommandKind.Scalar,
                    "SELECT COUNT(*) FROM st_session WHERE access_token={0} AND access_token={0}",
                    token)).Scalar;

                Assert.Equal(1, Convert.ToInt32(count, System.Globalization.CultureInfo.InvariantCulture));
            }
            finally
            {
                DeleteSessionRow(dbAccess, token);
            }
        }

        private static void InsertSessionRow(DbAccess dbAccess, Guid token, string xml, DateTime invalidTime)
        {
            dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                "INSERT INTO st_session (sys_rowid, access_token, session_user_xml, sys_insert_time, sys_invalid_time) " +
                "VALUES ({0}, {1}, {2}, {3}, {4})",
                Guid.NewGuid(), token, xml, DateTime.UtcNow, invalidTime));
        }

        private static void DeleteSessionRow(DbAccess dbAccess, Guid token)
        {
            try
            {
                dbAccess.Execute(new DbCommandSpec(DbCommandKind.NonQuery,
                    "DELETE FROM st_session WHERE access_token={0}", token));
            }
            catch (System.Data.Common.DbException ex)
            {
                // Best-effort: the row may never have been inserted when the test failed early.
                Console.WriteLine($"ParameterBindingOrderTests: cleanup of session {token} failed — {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
