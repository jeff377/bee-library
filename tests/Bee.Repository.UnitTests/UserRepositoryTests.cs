using System.ComponentModel;
using System.Data.Common;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// UserRepository 單元測試：驗證 GetRowIdBySysId 的純邏輯分支與 5 DB round-trip 行為。
    /// </summary>
    public class UserRepositoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public UserRepositoryTests(SharedDbFixture fx) { _fx = fx; }

        private UserRepository CreateRepo()
            => new UserRepository(_fx.GetRequiredService<IDbConnectionManager>());

        [Fact]
        [DisplayName("建構子傳入 null connectionManager 應拋出 ArgumentNullException")]
        public void Constructor_NullConnectionManager_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new UserRepository(null!));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("GetRowIdBySysId 傳入 null 或空白字串應直接回傳 Guid.Empty")]
        public void GetRowIdBySysId_NullOrWhitespace_ReturnsGuidEmpty(string? userId)
        {
            var repo = new UserRepository(new StubConnectionManager());
            var result = repo.GetRowIdBySysId(userId!);
            Assert.Equal(Guid.Empty, result);
        }

        private void RunRoundTrip(DatabaseType _)
        {
            var repo = CreateRepo();

            // seed user "001" 由 SharedDbFixture 預先建立
            var rowId = repo.GetRowIdBySysId("001");
            Assert.NotEqual(Guid.Empty, rowId);

            // 不存在的使用者 → Guid.Empty
            var missing = repo.GetRowIdBySysId("__nonexistent_user_xyz__");
            Assert.Equal(Guid.Empty, missing);
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetRowIdBySysId 存在的使用者回傳非空 RowId，不存在者回傳 Guid.Empty（SQL Server）")]
        public void RoundTrip_SqlServer() => RunRoundTrip(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("GetRowIdBySysId 存在的使用者回傳非空 RowId，不存在者回傳 Guid.Empty（PostgreSQL）")]
        public void RoundTrip_PostgreSql() => RunRoundTrip(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("GetRowIdBySysId 存在的使用者回傳非空 RowId，不存在者回傳 Guid.Empty（SQLite）")]
        public void RoundTrip_Sqlite() => RunRoundTrip(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("GetRowIdBySysId 存在的使用者回傳非空 RowId，不存在者回傳 Guid.Empty（MySQL）")]
        public void RoundTrip_MySql() => RunRoundTrip(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("GetRowIdBySysId 存在的使用者回傳非空 RowId，不存在者回傳 Guid.Empty（Oracle）")]
        public void RoundTrip_Oracle() => RunRoundTrip(DatabaseType.Oracle);

        private sealed class StubConnectionManager : IDbConnectionManager
        {
            public DbConnectionInfo GetConnectionInfo(string databaseId) => throw new NotSupportedException();
            public DbConnection CreateConnection(string databaseId) => throw new NotSupportedException();
            public bool Remove(string databaseId) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string databaseId) => throw new NotSupportedException();
            public int Count => throw new NotSupportedException();
        }
    }
}
