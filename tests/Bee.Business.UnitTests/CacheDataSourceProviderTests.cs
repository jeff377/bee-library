using System.ComponentModel;
using Bee.Business.Provider;
using Bee.Tests.Shared;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// <see cref="CacheDataSourceProvider"/> 覆蓋率補強測試。
    /// </summary>
    [Collection("Initialize")]
    public class CacheDataSourceProviderTests
    {
        [DbFact]
        [DisplayName("GetSessionUser 傳入不存在的 AccessToken 應回傳 null")]
        public void GetSessionUser_UnknownToken_ReturnsNull()
        {
            var provider = new CacheDataSourceProvider();

            var result = provider.GetSessionUser(Guid.NewGuid());

            Assert.Null(result);
        }
    }
}
