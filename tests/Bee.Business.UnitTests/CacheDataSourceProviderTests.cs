using System.ComponentModel;
using Bee.Business.Providers;
using Bee.Repository.Abstractions.Factories;
using Bee.Tests.Shared;
using Bee.Definition.Database;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="CacheDataSourceProvider"/> 測試。
    /// </summary>
    [Collection("Initialize")]
    public class CacheDataSourceProviderTests
    {
        [DbFact(DatabaseType.SQLServer)]
        [DisplayName("GetSessionUser 傳入不存在的 Token 應回傳 null")]
        public void GetSessionUser_UnknownToken_ReturnsNull()
        {
            var factory = BeeTestServices.GetRequiredService<ISystemRepositoryFactory>();
            var provider = new CacheDataSourceProvider(factory);

            var result = provider.GetSessionUser(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("CacheDataSourceProvider 建構子傳 null 應拋 ArgumentNullException")]
        public void Constructor_NullFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheDataSourceProvider(null!));
        }
    }
}
