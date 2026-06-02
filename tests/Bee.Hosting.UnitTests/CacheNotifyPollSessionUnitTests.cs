using System.ComponentModel;
using System.Reflection;
using Bee.Db;
using Bee.Definition.Database;
using Bee.Hosting.CacheNotify;
using Bee.ObjectCaching;
using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 純單元測試：驗證 <see cref="CacheNotifyPollSession"/> 建構子防護及私有靜態方法的邏輯，
    /// 不依賴資料庫，不依賴 DI 容器。
    /// </summary>
    public class CacheNotifyPollSessionUnitTests
    {
        private static readonly Type[] s_databaseTypeParam = [typeof(DatabaseType)];

        private sealed class StubDbAccessFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) => throw new NotSupportedException();
        }

        private sealed class StubCacheContainer : ICacheContainer
        {
            public SystemSettingsCache SystemSettings => throw new NotSupportedException();
            public DatabaseSettingsCache DatabaseSettings => throw new NotSupportedException();
            public ProgramSettingsCache ProgramSettings => throw new NotSupportedException();
            public DbCategorySettingsCache DbCategorySettings => throw new NotSupportedException();
            public TableSchemaCache TableSchema => throw new NotSupportedException();
            public FormSchemaCache FormSchema => throw new NotSupportedException();
            public FormLayoutCache FormLayout => throw new NotSupportedException();
            public LanguageResourceCache LanguageResource => throw new NotSupportedException();
            public SessionInfoCache SessionInfo => throw new NotSupportedException();
            public CompanyInfoCache CompanyInfo => throw new NotSupportedException();
            public bool TryEvict(string cacheKey) => false;
        }

        private static IDbAccessFactory NewFactory() => new StubDbAccessFactory();
        private static ICacheContainer NewContainer() => new StubCacheContainer();

        [Fact]
        [DisplayName("CacheNotifyPollSession 傳入 null databaseId 應拋 ArgumentException")]
        public void Ctor_NullDatabaseId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CacheNotifyPollSession(null!, NewFactory(), NewContainer(), 5));
        }

        [Fact]
        [DisplayName("CacheNotifyPollSession 傳入空白 databaseId 應拋 ArgumentException")]
        public void Ctor_WhiteSpaceDatabaseId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                new CacheNotifyPollSession("  ", NewFactory(), NewContainer(), 5));
        }

        [Fact]
        [DisplayName("CacheNotifyPollSession 傳入 null dbAccessFactory 應拋 ArgumentNullException")]
        public void Ctor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPollSession("db", null!, NewContainer(), 5));
        }

        [Fact]
        [DisplayName("CacheNotifyPollSession 傳入 null container 應拋 ArgumentNullException")]
        public void Ctor_NullContainer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPollSession("db", NewFactory(), null!, 5));
        }

        [Fact]
        [DisplayName("CacheNotifyPollSession 全部有效參數應可正常建立實例")]
        public void Ctor_AllValidArgs_CreatesInstance()
        {
            var exception = Record.Exception(() =>
                new CacheNotifyPollSession("db", NewFactory(), NewContainer(), 5));
            Assert.Null(exception);
        }

        [Fact]
        [DisplayName("CacheNotifyPollSession marginSeconds 為負數應修正為 0")]
        public void Ctor_NegativeMarginSeconds_NormalizesToZero()
        {
            var session = new CacheNotifyPollSession("db", NewFactory(), NewContainer(), -10);
            var marginField = typeof(CacheNotifyPollSession).GetField(
                "_margin", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(marginField);
            var margin = (TimeSpan)marginField!.GetValue(session)!;
            Assert.Equal(TimeSpan.Zero, margin);
        }

        [Theory]
        [InlineData(DatabaseType.SQLServer, "SELECT getdate()")]
        [InlineData(DatabaseType.PostgreSQL, "SELECT LOCALTIMESTAMP")]
        [InlineData(DatabaseType.MySQL, "SELECT CURRENT_TIMESTAMP(6)")]
        [InlineData(DatabaseType.Oracle, "SELECT LOCALTIMESTAMP FROM dual")]
        [InlineData(DatabaseType.SQLite, "SELECT CURRENT_TIMESTAMP")]
        [DisplayName("NaiveNowCommandText 各資料庫類型應回傳對應 SQL 字串")]
        public void NaiveNowCommandText_KnownDatabaseType_ReturnsExpectedSql(DatabaseType dbType, string expected)
        {
            var method = typeof(CacheNotifyPollSession).GetMethod(
                "NaiveNowCommandText",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_databaseTypeParam,
                null);
            Assert.NotNull(method);
            var result = method!.Invoke(null, new object[] { dbType }) as string;
            Assert.Equal(expected, result);
        }

        [Fact]
        [DisplayName("NaiveNowCommandText 未知資料庫類型應拋 NotSupportedException")]
        public void NaiveNowCommandText_UnknownDatabaseType_ThrowsNotSupportedException()
        {
            var method = typeof(CacheNotifyPollSession).GetMethod(
                "NaiveNowCommandText",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_databaseTypeParam,
                null);
            Assert.NotNull(method);
            var ex = Record.Exception(() => method!.Invoke(null, new object[] { (DatabaseType)999 }));
            Assert.NotNull(ex);
            Assert.IsType<NotSupportedException>(ex.InnerException ?? ex);
        }

        [Theory]
        [InlineData(DatabaseType.SQLServer, "yyyy-MM-ddTHH:mm:ss.fffffff", "CAST({0} AS datetime2)")]
        [InlineData(DatabaseType.PostgreSQL, "yyyy-MM-ddTHH:mm:ss.ffffff", "CAST({0} AS timestamp)")]
        [InlineData(DatabaseType.MySQL, "yyyy-MM-dd HH:mm:ss.ffffff", "CAST({0} AS DATETIME(6))")]
        [InlineData(DatabaseType.Oracle, "yyyy-MM-ddTHH:mm:ss.ffffff", "TO_TIMESTAMP({0}, 'YYYY-MM-DD\"T\"HH24:MI:SS.FF6')")]
        [InlineData(DatabaseType.SQLite, "yyyy-MM-dd HH:mm:ss", "{0}")]
        [DisplayName("ThresholdBinding 各資料庫類型應回傳對應日期格式與 SQL 樣板")]
        public void ThresholdBinding_KnownDatabaseType_ReturnsExpectedFormatAndTemplate(
            DatabaseType dbType, string expectedFormat, string expectedCastTemplate)
        {
            var method = typeof(CacheNotifyPollSession).GetMethod(
                "ThresholdBinding",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_databaseTypeParam,
                null);
            Assert.NotNull(method);
            var result = method!.Invoke(null, new object[] { dbType });
            Assert.NotNull(result);
            var resultType = result!.GetType();
            var format = (string)resultType.GetField("Item1")!.GetValue(result)!;
            var castTemplate = (string)resultType.GetField("Item2")!.GetValue(result)!;
            Assert.Equal(expectedFormat, format);
            Assert.Equal(expectedCastTemplate, castTemplate);
        }

        [Fact]
        [DisplayName("ThresholdBinding 未知資料庫類型應拋 NotSupportedException")]
        public void ThresholdBinding_UnknownDatabaseType_ThrowsNotSupportedException()
        {
            var method = typeof(CacheNotifyPollSession).GetMethod(
                "ThresholdBinding",
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                s_databaseTypeParam,
                null);
            Assert.NotNull(method);
            var ex = Record.Exception(() => method!.Invoke(null, new object[] { (DatabaseType)999 }));
            Assert.NotNull(ex);
            Assert.IsType<NotSupportedException>(ex.InnerException ?? ex);
        }
    }
}
