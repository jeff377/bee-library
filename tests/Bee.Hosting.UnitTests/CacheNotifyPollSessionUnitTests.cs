using System.ComponentModel;
using Bee.Db.CacheNotify;
using Bee.Hosting.CacheNotify;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// <see cref="CacheNotifyPollSession"/> 建構子防衛式斷言。
    /// 五家方言的 SQL 分支已下沉至 <c>Bee.Db</c> 的 <c>CacheNotifyReader</c>，
    /// 對應測試見 <c>Bee.Db.UnitTests.CacheNotifyReaderUnitTests</c>。
    /// </summary>
    public class CacheNotifyPollSessionUnitTests
    {
        private sealed class StubReader : ICacheNotifyReader
        {
            public DateTime ReadBaseline(string databaseId) => throw new NotImplementedException();

            public IReadOnlyList<CacheNotifyChange> ReadChangesSince(string databaseId, DateTime threshold)
                => throw new NotImplementedException();
        }

        private static readonly ICacheNotifyReader s_reader = new StubReader();

        [Fact]
        [DisplayName("CacheNotifyPollSession 建構子 databaseId 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullDatabaseId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPollSession(null!, s_reader, marginSeconds: 0));
        }

        [Fact]
        [DisplayName("CacheNotifyPollSession 建構子 databaseId 為空白字串應拋 ArgumentException")]
        public void Constructor_WhitespaceDatabaseId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CacheNotifyPollSession("   ", s_reader, marginSeconds: 0));
        }

        [Fact]
        [DisplayName("CacheNotifyPollSession 建構子 reader 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullReader_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPollSession("test_db", null!, marginSeconds: 0));
        }

        [Fact]
        [DisplayName("CacheNotifyPollSession 建構子 marginSeconds 為負值時應成功建立（正規化為 0）")]
        public void Constructor_NegativeMarginSeconds_CreatesInstanceWithoutThrowing()
        {
            var exception = Record.Exception(() =>
                new CacheNotifyPollSession("test_db", s_reader, marginSeconds: -1));
            Assert.Null(exception);
        }
    }
}
