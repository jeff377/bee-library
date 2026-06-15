using System.ComponentModel;
using System.Globalization;
using Bee.Definition.Database;
using Bee.Tests.Shared;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Verifies that <see cref="SharedDatabaseState"/>'s Northwind business-table seed
    /// actually populated the <c>company</c> database (counts, forward-relation resolution,
    /// and the master-detail link). A passing suite alone would not catch a silently
    /// no-op'd seed, so these assertions exercise the seeded rows directly. The master-detail
    /// check also covers the SQLite GUID case-insensitive comparison (sys_master_rowid is a
    /// GUID key compared across the seed's casing).
    /// </summary>
    public class NorthwindSeedTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public NorthwindSeedTests(SharedDbFixture fx) { _fx = fx; }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("SQL Server 上 Northwind seed 應建立完整業務資料與 master-detail 連結")]
        public void Seed_SqlServer() => AssertSeed(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("PostgreSQL 上 Northwind seed 應建立完整業務資料與 master-detail 連結")]
        public void Seed_PostgreSql() => AssertSeed(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("SQLite 上 Northwind seed 應建立完整業務資料與 master-detail 連結")]
        public void Seed_Sqlite() => AssertSeed(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("MySQL 上 Northwind seed 應建立完整業務資料與 master-detail 連結")]
        public void Seed_MySql() => AssertSeed(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Oracle 上 Northwind seed 應建立完整業務資料與 master-detail 連結")]
        public void Seed_Oracle() => AssertSeed(DatabaseType.Oracle);

        private void AssertSeed(DatabaseType dbType)
        {
            var db = _fx.NewDbAccess(TestDbConventions.GetDatabaseId(dbType, "company"));

            // Row counts match the seed JSON (5 orders, 12 order details, 15 products).
            Assert.Equal(5, Count(dbType, db, "ft_order"));
            Assert.Equal(12, Count(dbType, db, "ft_order_detail"));
            Assert.Equal(15, Count(dbType, db, "ft_product"));

            // Forward relation resolved: order 10248 carries a non-empty customer_rowid that
            // points at customer ALFKI's sys_rowid.
            var orderRowId = ScalarGuid(dbType, db, "ft_order", "sys_rowid", "sys_id", "10248");
            Assert.NotEqual(Guid.Empty, orderRowId);
            var customerRowId = ScalarGuid(dbType, db, "ft_customer", "sys_rowid", "sys_id", "ALFKI");
            var orderCustomerRowId = ScalarGuid(dbType, db, "ft_order", "customer_rowid", "sys_id", "10248");
            Assert.Equal(customerRowId, orderCustomerRowId);

            // Master-detail: order 10248 has exactly 2 detail rows linked by sys_master_rowid.
            string detailTable = dbType.QuoteIdentifier("ft_order_detail");
            string masterCol = dbType.QuoteIdentifier("sys_master_rowid");
            var detailCount = db.Execute(new DbCommandSpec(DbCommandKind.Scalar,
                $"SELECT COUNT(*) FROM {detailTable} WHERE {masterCol} = {{0}}", orderRowId)).Scalar;
            Assert.Equal(2, Convert.ToInt32(detailCount, CultureInfo.InvariantCulture));
        }

        private static int Count(DatabaseType dbType, DbAccess db, string table)
        {
            var result = db.Execute(new DbCommandSpec(DbCommandKind.Scalar,
                $"SELECT COUNT(*) FROM {dbType.QuoteIdentifier(table)}")).Scalar;
            return Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }

        private static Guid ScalarGuid(
            DatabaseType dbType, DbAccess db, string table, string selectCol, string keyCol, string keyValue)
        {
            var result = db.Execute(new DbCommandSpec(DbCommandKind.Scalar,
                $"SELECT {dbType.QuoteIdentifier(selectCol)} FROM {dbType.QuoteIdentifier(table)} " +
                $"WHERE {dbType.QuoteIdentifier(keyCol)} = {{0}}", keyValue)).Scalar;
            return result switch
            {
                Guid g => g,
                byte[] { Length: 16 } b => new Guid(b),
                string s when Guid.TryParse(s, out var g) => g,
                _ => Guid.Empty,
            };
        }
    }
}
