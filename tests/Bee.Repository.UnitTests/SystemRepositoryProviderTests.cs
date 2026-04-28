using System.ComponentModel;
using Bee.Definition.Settings;
using Bee.Repository.Abstractions.System;
using Bee.Repository.Providers;
using Bee.Repository.System;
using Bee.Definition.Identity;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 針對 <see cref="SystemRepositoryProvider"/> 預設成員與覆寫能力的純邏輯測試。
    /// </summary>
    [Collection("Initialize")]
    public class SystemRepositoryProviderTests
    {
        [Fact]
        [DisplayName("SystemRepositoryProvider 預設 DatabaseRepository 應非 null 且實作 IDatabaseRepository")]
        public void Constructor_DefaultDatabaseRepository_ImplementsInterface()
        {
            var provider = new SystemRepositoryProvider();

            Assert.NotNull(provider.DatabaseRepository);
            Assert.IsType<IDatabaseRepository>(provider.DatabaseRepository, exactMatch: false);
        }

        [Fact]
        [DisplayName("SystemRepositoryProvider 預設 SessionRepository 應為 SessionRepository 型別")]
        public void Constructor_DefaultSessionRepository_IsSessionRepositoryType()
        {
            var provider = new SystemRepositoryProvider();

            Assert.IsType<SessionRepository>(provider.SessionRepository);
        }

        [Fact]
        [DisplayName("SystemRepositoryProvider.DatabaseRepository 可被替換為測試替身")]
        public void DatabaseRepository_CanBeReplaced()
        {
            var provider = new SystemRepositoryProvider();
            var stub = new StubDatabaseRepository();

            provider.DatabaseRepository = stub;

            Assert.Same(stub, provider.DatabaseRepository);
        }

        [Fact]
        [DisplayName("SystemRepositoryProvider.SessionRepository 可被替換為測試替身")]
        public void SessionRepository_CanBeReplaced()
        {
            var provider = new SystemRepositoryProvider();
            var stub = new StubSessionRepository();

            provider.SessionRepository = stub;

            Assert.Same(stub, provider.SessionRepository);
        }

        private sealed class StubDatabaseRepository : IDatabaseRepository
        {
            public void TestConnection(DatabaseItem item) { }
            public bool UpgradeTableSchema(string databaseId, string dbName, string tableName) => false;
        }

        private sealed class StubSessionRepository : ISessionRepository
        {
            public SessionUser? GetSession(Guid accessToken) => null;
            public SessionUser CreateSession(string userID, int expiresIn = 3600, bool oneTime = false)
                => new SessionUser();
        }
    }
}
