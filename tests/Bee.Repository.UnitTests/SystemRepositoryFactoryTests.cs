using System.ComponentModel;
using Bee.Repository.Abstractions.System;
using Bee.Repository.Factories;
using Bee.Repository.System;

namespace Bee.Repository.UnitTests
{
    /// <summary>
    /// 針對 <see cref="SystemRepositoryFactory"/> 工廠方法的純邏輯測試。
    /// </summary>
    [Collection("Initialize")]
    public class SystemRepositoryFactoryTests
    {
        [Fact]
        [DisplayName("CreateDatabaseRepository 應回傳 DatabaseRepository 型別")]
        public void CreateDatabaseRepository_ReturnsDatabaseRepositoryType()
        {
            var factory = new SystemRepositoryFactory();

            var repo = factory.CreateDatabaseRepository();

            Assert.NotNull(repo);
            Assert.IsType<IDatabaseRepository>(repo, exactMatch: false);
        }

        [Fact]
        [DisplayName("CreateSessionRepository 應回傳 SessionRepository 型別")]
        public void CreateSessionRepository_ReturnsSessionRepositoryType()
        {
            var factory = new SystemRepositoryFactory();

            var repo = factory.CreateSessionRepository();

            Assert.NotNull(repo);
            Assert.IsType<SessionRepository>(repo);
        }

        [Fact]
        [DisplayName("CreateDatabaseRepository 每次呼叫應回傳新實例")]
        public void CreateDatabaseRepository_EachCallReturnsNewInstance()
        {
            var factory = new SystemRepositoryFactory();

            var first = factory.CreateDatabaseRepository();
            var second = factory.CreateDatabaseRepository();

            Assert.NotSame(first, second);
        }

        [Fact]
        [DisplayName("CreateSessionRepository 每次呼叫應回傳新實例")]
        public void CreateSessionRepository_EachCallReturnsNewInstance()
        {
            var factory = new SystemRepositoryFactory();

            var first = factory.CreateSessionRepository();
            var second = factory.CreateSessionRepository();

            Assert.NotSame(first, second);
        }
    }
}
