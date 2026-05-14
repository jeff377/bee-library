using System.ComponentModel;
using Bee.Db.Dml;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// Pure-string unit tests for <see cref="LimitBuilder"/>. Covers all five dialects
    /// across the documented (skip, take) boundary cases plus two argument-validation
    /// cases. No database dependency.
    /// </summary>
    public class LimitBuilderTests
    {
        // -------- SQL Server (OFFSET ... ROWS FETCH NEXT ... ROWS ONLY) --------

        [Theory]
        [InlineData(null, null, "")]
        [InlineData(0, 10, "OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY")]
        [InlineData(10, 10, "OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY")]
        [InlineData(5, null, "OFFSET 5 ROWS")]
        [InlineData(null, 3, "OFFSET 0 ROWS FETCH NEXT 3 ROWS ONLY")]
        [DisplayName("SQL Server：分頁子句 OFFSET/FETCH 5 邊界 case")]
        public void Build_SqlServer(int? skip, int? take, string expected)
        {
            var builder = new LimitBuilder(DatabaseType.SQLServer);
            Assert.Equal(expected, builder.Build(skip, take));
        }

        // -------- Oracle 12c+ (same OFFSET/FETCH as SQL Server) --------

        [Theory]
        [InlineData(null, null, "")]
        [InlineData(0, 10, "OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY")]
        [InlineData(10, 10, "OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY")]
        [InlineData(5, null, "OFFSET 5 ROWS")]
        [InlineData(null, 3, "OFFSET 0 ROWS FETCH NEXT 3 ROWS ONLY")]
        [DisplayName("Oracle：分頁子句 OFFSET/FETCH 5 邊界 case")]
        public void Build_Oracle(int? skip, int? take, string expected)
        {
            var builder = new LimitBuilder(DatabaseType.Oracle);
            Assert.Equal(expected, builder.Build(skip, take));
        }

        // -------- PostgreSQL (LIMIT / OFFSET independent) --------

        [Theory]
        [InlineData(null, null, "")]
        [InlineData(0, 10, "LIMIT 10 OFFSET 0")]
        [InlineData(10, 10, "LIMIT 10 OFFSET 10")]
        [InlineData(5, null, "OFFSET 5")]
        [InlineData(null, 3, "LIMIT 3")]
        [DisplayName("PostgreSQL：分頁子句 LIMIT/OFFSET 5 邊界 case")]
        public void Build_PostgreSql(int? skip, int? take, string expected)
        {
            var builder = new LimitBuilder(DatabaseType.PostgreSQL);
            Assert.Equal(expected, builder.Build(skip, take));
        }

        // -------- SQLite (LIMIT / OFFSET independent) --------

        [Theory]
        [InlineData(null, null, "")]
        [InlineData(0, 10, "LIMIT 10 OFFSET 0")]
        [InlineData(10, 10, "LIMIT 10 OFFSET 10")]
        [InlineData(5, null, "OFFSET 5")]
        [InlineData(null, 3, "LIMIT 3")]
        [DisplayName("SQLite：分頁子句 LIMIT/OFFSET 5 邊界 case")]
        public void Build_Sqlite(int? skip, int? take, string expected)
        {
            var builder = new LimitBuilder(DatabaseType.SQLite);
            Assert.Equal(expected, builder.Build(skip, take));
        }

        // -------- MySQL (OFFSET requires LIMIT; uses UINT64_MAX sentinel) --------

        [Theory]
        [InlineData(null, null, "")]
        [InlineData(0, 10, "LIMIT 10 OFFSET 0")]
        [InlineData(10, 10, "LIMIT 10 OFFSET 10")]
        [InlineData(5, null, "LIMIT 18446744073709551615 OFFSET 5")]
        [InlineData(null, 3, "LIMIT 3")]
        [DisplayName("MySQL：分頁子句 LIMIT/OFFSET 5 邊界 case（含 UINT64_MAX sentinel）")]
        public void Build_MySql(int? skip, int? take, string expected)
        {
            var builder = new LimitBuilder(DatabaseType.MySQL);
            Assert.Equal(expected, builder.Build(skip, take));
        }

        // -------- Boundary cases --------

        [Fact]
        [DisplayName("Build 傳入負數 skip 應丟出 ArgumentOutOfRangeException")]
        public void Build_NegativeSkip_Throws()
        {
            var builder = new LimitBuilder(DatabaseType.SQLServer);
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Build(-1, 10));
        }

        [Fact]
        [DisplayName("Build 傳入負數 take 應丟出 ArgumentOutOfRangeException")]
        public void Build_NegativeTake_Throws()
        {
            var builder = new LimitBuilder(DatabaseType.SQLServer);
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Build(0, -1));
        }
    }
}
