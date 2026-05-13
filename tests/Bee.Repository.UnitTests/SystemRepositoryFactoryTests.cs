using System.ComponentModel;
using Bee.Repository.Abstractions.Factories;
using Bee.Repository.Abstractions.System;
using Bee.Repository.Factories;
using Bee.Repository.System;
using Bee.Tests.Shared;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 針對 <see cref="SystemRepositoryFactory"/> 工廠方法的純邏輯測試。
    /// </summary>
    public class SystemRepositoryFactoryTests : IClassFixture<SharedDbFixture>
    {
        private readonly SharedDbFixture _fx;

        public SystemRepositoryFactoryTests(SharedDbFixture fx) { _fx = fx; }
        private ISystemRepositoryFactory Factory => _fx.GetRequiredService<ISystemRepositoryFactory>();

        [Fact]
        [DisplayName("CreateDatabaseRepository 應回傳 DatabaseRepository 型別")]
        public void CreateDatabaseRepository_ReturnsDatabaseRepositoryType()
        {
            var repo = Factory.CreateDatabaseRepository();

            Assert.NotNull(repo);
            Assert.IsType<IDatabaseRepository>(repo, exactMatch: false);
        }

        [Fact]
        [DisplayName("CreateSessionRepository 應回傳 SessionRepository 型別")]
        public void CreateSessionRepository_ReturnsSessionRepositoryType()
        {
            var repo = Factory.CreateSessionRepository();

            Assert.NotNull(repo);
            Assert.IsType<SessionRepository>(repo);
        }

        [Fact]
        [DisplayName("CreateDatabaseRepository 每次呼叫應回傳新實例")]
        public void CreateDatabaseRepository_EachCallReturnsNewInstance()
        {
            var first = Factory.CreateDatabaseRepository();
            var second = Factory.CreateDatabaseRepository();

            Assert.NotSame(first, second);
        }

        [Fact]
        [DisplayName("CreateSessionRepository 每次呼叫應回傳新實例")]
        public void CreateSessionRepository_EachCallReturnsNewInstance()
        {
            var first = Factory.CreateSessionRepository();
            var second = Factory.CreateSessionRepository();

            Assert.NotSame(first, second);
        }

        [Fact]
        [DisplayName("SystemRepositoryFactory 直接構造傳入 null IDefineAccess 應拋 ArgumentNullException")]
        public void Ctor_NullDefineAccess_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new SystemRepositoryFactory(null!));
        }
    }
}
