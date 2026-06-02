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
    /// <see cref="CacheNotifyPollSession"/> 純邏輯路徑的單元測試（不依賴資料庫）：
    /// 建構式引數驗證、負數 margin 的截斷行為，以及 SQLite 與不支援 DatabaseType 的 switch 分支。
    /// </summary>
    public class CacheNotifyPollSessionUnitTests
    {
        private static FakeDbAccessFactory ValidFactory => new();
        private static FakeCacheContainer ValidContainer => new();

        // 以反射呼叫私有靜態方法 NaiveNowCommandText
        private static string InvokeNaiveNowCommandText(DatabaseType databaseType)
        {
            var method = typeof(CacheNotifyPollSession).GetMethod(
                "NaiveNowCommandText",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return (string)method!.Invoke(null, new object[] { databaseType })!;
        }

        // 以反射呼叫私有靜態方法 ThresholdBinding
        private static object InvokeThresholdBinding(DatabaseType databaseType)
        {
            var method = typeof(CacheNotifyPollSession).GetMethod(
                "ThresholdBinding",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return method!.Invoke(null, new object[] { databaseType })!;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        [DisplayName("CacheNotifyPollSession 傳入 null 或空白 databaseId 應拋 ArgumentException")]
        public void Constructor_NullOrWhitespaceDatabaseId_ThrowsArgumentException(string? databaseId)
        {
            Assert.Throws<ArgumentException>(() =>
                new CacheNotifyPollSession(databaseId!, ValidFactory, ValidContainer, 5));
        }

        [Fact]
        [DisplayName("CacheNotifyPollSession 傳入 null dbAccessFactory 應拋 ArgumentNullException")]
        public void Constructor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPollSession("common", null!, ValidContainer, 5));
        }

        [Fact]
        [DisplayName("CacheNotifyPollSession 傳入 null container 應拋 ArgumentNullException")]
        public void Constructor_NullContainer_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CacheNotifyPollSession("common", ValidFactory, null!, 5));
        }

        [Fact]
        [DisplayName("CacheNotifyPollSession marginSeconds 為負數時應以 TimeSpan.Zero 作為 margin")]
        public void Constructor_NegativeMarginSeconds_UsesZeroMargin()
        {
            var session = new CacheNotifyPollSession("common", ValidFactory, ValidContainer, -1);
            var marginField = typeof(CacheNotifyPollSession).GetField(
                "_margin", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(marginField);
            var margin = (TimeSpan)marginField!.GetValue(session)!;
            Assert.Equal(TimeSpan.Zero, margin);
        }

        [Fact]
        [DisplayName("NaiveNowCommandText SQLite 應回傳 SELECT CURRENT_TIMESTAMP")]
        public void NaiveNowCommandText_SQLite_ReturnsCurrentTimestampQuery()
        {
            var result = InvokeNaiveNowCommandText(DatabaseType.SQLite);
            Assert.Equal("SELECT CURRENT_TIMESTAMP", result);
        }

        [Fact]
        [DisplayName("NaiveNowCommandText 不支援的 DatabaseType 應拋 NotSupportedException")]
        public void NaiveNowCommandText_UnsupportedDatabaseType_ThrowsNotSupportedException()
        {
            var ex = Record.Exception(() => InvokeNaiveNowCommandText((DatabaseType)99));
            Assert.NotNull(ex);
            Assert.IsType<NotSupportedException>(ex.InnerException ?? ex);
        }

        [Fact]
        [DisplayName("ThresholdBinding SQLite 應回傳 yyyy-MM-dd HH:mm:ss 格式與直通 castTemplate")]
        public void ThresholdBinding_SQLite_ReturnsExpectedFormatAndTemplate()
        {
            var result = InvokeThresholdBinding(DatabaseType.SQLite);
            Assert.NotNull(result);
            var resultStr = result.ToString()!;
            Assert.Contains("yyyy-MM-dd HH:mm:ss", resultStr);
        }

        [Fact]
        [DisplayName("ThresholdBinding 不支援的 DatabaseType 應拋 NotSupportedException")]
        public void ThresholdBinding_UnsupportedDatabaseType_ThrowsNotSupportedException()
        {
            var ex = Record.Exception(() => InvokeThresholdBinding((DatabaseType)99));
            Assert.NotNull(ex);
            Assert.IsType<NotSupportedException>(ex.InnerException ?? ex);
        }

        private sealed class FakeDbAccessFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) => throw new NotImplementedException();
        }

        private sealed class FakeCacheContainer : ICacheContainer
        {
            public SystemSettingsCache SystemSettings => null!;
            public DatabaseSettingsCache DatabaseSettings => null!;
            public ProgramSettingsCache ProgramSettings => null!;
            public DbCategorySettingsCache DbCategorySettings => null!;
            public TableSchemaCache TableSchema => null!;
            public FormSchemaCache FormSchema => null!;
            public FormLayoutCache FormLayout => null!;
            public LanguageResourceCache LanguageResource => null!;
            public SessionInfoCache SessionInfo => null!;
            public CompanyInfoCache CompanyInfo => null!;
            public bool TryEvict(string cacheKey) => false;
        }
    }
}
