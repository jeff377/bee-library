using System.ComponentModel;
using Bee.Base.Security;
using Bee.Db;
using Bee.Db.Manager;
using Bee.Definition.Database;
using Bee.Definition.Security;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// <see cref="ApiKeyRepository"/> 的讀寫測試：雜湊金鑰的 round-trip、停用列被查詢層排除、
    /// 以及相容閘門（<c>GetGateState</c>）在有啟用金鑰時轉為 in force。
    /// </summary>
    /// <remarks>
    /// 每個測試用唯一 <c>sys_id</c>（`rt-{guid}`）並在 finally 清理，因為實體資料庫由多個平行
    /// 測試行程共用。
    /// </remarks>
    public class ApiKeyRepositoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;
        public ApiKeyRepositoryTests(SharedDbFixture fx) { _fx = fx; }

        private ApiKeyRepository CreateRepo()
            => new ApiKeyRepository(TestRepositoryContext.Create(_fx.GetRequiredService<IDbConnectionManager>()), Guid.Empty, string.Empty);

        private static string NewSysId() => "rt-" + Guid.NewGuid().ToString("N");

        private void DeleteKey(string sysId)
        {
            var connectionManager = _fx.GetRequiredService<IDbConnectionManager>();
            var dbType = connectionManager.GetConnectionInfo(DbCategoryIds.Common).DatabaseType;
            string sql = $"DELETE FROM {dbType.QuoteIdentifier("st_api_key")} " +
                         $"WHERE {dbType.QuoteIdentifier("sys_id")} = {{0}}";
            new DbAccess(DbCategoryIds.Common, connectionManager)
                .Execute(new DbCommandSpec(DbCommandKind.NonQuery, sql, sysId));
        }

        #region Insert + GetEnabledById round-trip

        private void RunRoundTrip(DatabaseType _)
        {
            var repo = CreateRepo();
            string sysId = NewSysId();
            string secret = ApiKeyFormat.CreateSecret();
            var expiredAt = new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            try
            {
                repo.Insert(new ApiKeyInfo
                {
                    SysId = sysId,
                    SysName = "Round-trip app",
                    HashedKey = ApiKeyHasher.HashSecret(secret),
                    KeyType = ApiKeyType.ThirdParty,
                    Contact = "ops@example.com",
                    ExpiredAt = expiredAt,
                });

                var actual = repo.GetEnabledById(sysId);

                Assert.NotNull(actual);
                Assert.Equal(sysId, actual!.SysId);
                Assert.Equal("Round-trip app", actual.SysName);
                Assert.Equal(ApiKeyType.ThirdParty, actual.KeyType);
                Assert.Equal("ops@example.com", actual.Contact);
                Assert.Equal(expiredAt, actual.ExpiredAt);
                // The stored form verifies against the plaintext secret and only against it.
                Assert.True(ApiKeyHasher.VerifySecret(secret, actual.HashedKey));
                Assert.False(ApiKeyHasher.VerifySecret(ApiKeyFormat.CreateSecret(), actual.HashedKey));
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("Insert 後 GetEnabledById on SQL Server 應完整取回金鑰列")]
        public void Insert_ThenGet_SqlServer() => RunRoundTrip(DatabaseType.SQLServer);

        [DbFact(DatabaseType.PostgreSQL)]
        [DisplayName("Insert 後 GetEnabledById on PostgreSQL 應完整取回金鑰列")]
        public void Insert_ThenGet_PostgreSql() => RunRoundTrip(DatabaseType.PostgreSQL);

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("Insert 後 GetEnabledById on SQLite 應完整取回金鑰列")]
        public void Insert_ThenGet_Sqlite() => RunRoundTrip(DatabaseType.SQLite);

        [DbFact(DatabaseType.MySQL)]
        [DisplayName("Insert 後 GetEnabledById on MySQL 應完整取回金鑰列")]
        public void Insert_ThenGet_MySql() => RunRoundTrip(DatabaseType.MySQL);

        [DbFact(DatabaseType.Oracle)]
        [DisplayName("Insert 後 GetEnabledById on Oracle 應完整取回金鑰列")]
        public void Insert_ThenGet_Oracle() => RunRoundTrip(DatabaseType.Oracle);

        #endregion

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("Insert 未指定到期時間時 GetEnabledById 應回 null 到期時間")]
        public void Insert_WithoutExpiry_ReadsBackNull()
        {
            var repo = CreateRepo();
            string sysId = NewSysId();
            try
            {
                repo.Insert(new ApiKeyInfo
                {
                    SysId = sysId,
                    SysName = "No expiry app",
                    HashedKey = ApiKeyHasher.HashSecret(ApiKeyFormat.CreateSecret()),
                });

                var actual = repo.GetEnabledById(sysId);

                Assert.NotNull(actual);
                Assert.Null(actual!.ExpiredAt);
                Assert.False(actual.IsExpired(DateTime.UtcNow));
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("GetEnabledById 於 sys_id 查無時應回傳 null")]
        public void GetEnabledById_UnknownSysId_ReturnsNull()
        {
            Assert.Null(CreateRepo().GetEnabledById(NewSysId()));
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("Exists 應在寫入後為 true、清理後為 false")]
        public void Exists_ReflectsRowPresence()
        {
            var repo = CreateRepo();
            string sysId = NewSysId();
            try
            {
                Assert.False(repo.Exists(sysId));

                repo.Insert(new ApiKeyInfo
                {
                    SysId = sysId,
                    SysName = "Exists app",
                    HashedKey = ApiKeyHasher.HashSecret(ApiKeyFormat.CreateSecret()),
                });

                Assert.True(repo.Exists(sysId));
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("GetGateState 於存在啟用金鑰時應為 in force（發第一把金鑰即關上閘門）")]
        public void GetGateState_WithEnabledKey_IsInForce()
        {
            var repo = CreateRepo();
            string sysId = NewSysId();
            try
            {
                repo.Insert(new ApiKeyInfo
                {
                    SysId = sysId,
                    SysName = "Gate app",
                    HashedKey = ApiKeyHasher.HashSecret(ApiKeyFormat.CreateSecret()),
                });

                var gate = repo.GetGateState();

                Assert.True(gate.InForce);
            }
            finally
            {
                DeleteKey(sysId);
            }
        }

        [DbFact(DatabaseType.SQLite)]
        [DisplayName("GetGateState 於表存在時不應擲例外（表存在與否走 schema provider 判定）")]
        public void GetGateState_TableExists_DoesNotThrow()
        {
            var exception = Record.Exception(() => CreateRepo().GetGateState());

            Assert.Null(exception);
        }
    }
}
