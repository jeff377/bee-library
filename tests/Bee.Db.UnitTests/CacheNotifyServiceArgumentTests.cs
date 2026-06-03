using System.ComponentModel;
using Bee.Db.CacheNotify;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// 針對 <see cref="CacheNotifyService"/> 的引數驗證純單元測試（不需資料庫連線）。
    /// 補強 <c>TouchAsync</c> 的引數守衛覆蓋率：既有 DB 整合測試只呼叫
    /// <c>Touch</c>，<c>TouchAsync</c> 的守衛行從未被執行。
    /// </summary>
    public class CacheNotifyServiceArgumentTests
    {
        private static readonly CacheNotifyService s_service = new();

        #region TouchAsync 引數守衛

        [Fact]
        [DisplayName("TouchAsync: null cacheKey 應丟 ArgumentNullException")]
        public async Task TouchAsync_NullCacheKey_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => s_service.TouchAsync(null!, null!, DatabaseType.SQLServer));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("TouchAsync: 空白 cacheKey 應丟 ArgumentException")]
        public async Task TouchAsync_WhitespaceCacheKey_ThrowsArgumentException(string cacheKey)
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => s_service.TouchAsync(cacheKey, null!, DatabaseType.SQLServer));
        }

        [Fact]
        [DisplayName("TouchAsync: null transaction 應丟 ArgumentNullException")]
        public async Task TouchAsync_NullTransaction_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => s_service.TouchAsync("key", null!, DatabaseType.SQLServer));
        }

        #endregion

        #region Touch 引數守衛

        [Fact]
        [DisplayName("Touch: null cacheKey 應丟 ArgumentNullException")]
        public void Touch_NullCacheKey_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => s_service.Touch(null!, null!, DatabaseType.SQLServer));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("Touch: 空白 cacheKey 應丟 ArgumentException")]
        public void Touch_WhitespaceCacheKey_ThrowsArgumentException(string cacheKey)
        {
            Assert.Throws<ArgumentException>(() => s_service.Touch(cacheKey, null!, DatabaseType.SQLServer));
        }

        [Fact]
        [DisplayName("Touch: null transaction 應丟 ArgumentNullException")]
        public void Touch_NullTransaction_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => s_service.Touch("key", null!, DatabaseType.SQLServer));
        }

        #endregion
    }
}
