using System.ComponentModel;
using Bee.Business.Providers;
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
            var provider = new CacheDataSourceProvider();

            var result = provider.GetSessionUser(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        [DisplayName("CacheDataSourceProvider 建構子應正常初始化")]
        public void Constructor_Default_CreatesInstance()
        {
            var exception = Record.Exception(() => new CacheDataSourceProvider());

            Assert.Null(exception);
        }
    }
}
