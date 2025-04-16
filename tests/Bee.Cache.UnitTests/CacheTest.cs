using Bee.Base;
using Bee.Define;

namespace Bee.Cache.UnitTests
{
    public class CacheTest
    {
        static CacheTest()
        {
            BackendInfo.DefinePath = @"D:\Bee\src\DefinePath";
        }

        [Fact]
        public void DatabaseSettingsCache()
        {
            var settings = CacheFunc.GetDatabaseSettings();
            for (int i = 0; i < 10; i++)
            {
                var cache = CacheFunc.GetDatabaseSettings();
                Assert.Equal(settings, cache);
            }
        }
    }
}