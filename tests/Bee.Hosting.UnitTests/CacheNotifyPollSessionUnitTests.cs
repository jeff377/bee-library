using System.ComponentModel;
using System.Reflection;
using Bee.Db;
using Bee.Definition.Database;
using Bee.Hosting.CacheNotify;
using Bee.ObjectCaching;
using Bee.ObjectCaching.CacheNotify;
using Bee.ObjectCaching.Database;
using Bee.ObjectCaching.Define;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 純單元測試：以 reflection 覆蓋 <see cref="CacheNotifyPollSession"/> 的
    /// SQLite 路徑與不支援的資料庫類型分支，以及建構子 marginSeconds 負數 clamp 行為。
    /// 不需要連接真實資料庫。
    /// </summary>
    public class CacheNotifyPollSessionUnitTests
    {
        private static readonly DatabaseType s_unsupportedDbType = (DatabaseType)99;

        private sealed class StubDbFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) => throw new InvalidOperationException("stub");
        }

        private sealed class StubContainer : ICacheContainer
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
        }

        private static string InvokeNaiveNowCommandText(DatabaseType databaseType)
        {
            var method = typeof(CacheNotifyPollSession).GetMethod(
                "NaiveNowCommandText", BindingFlags.NonPublic | BindingFlags.Static);
            return (string)method!.Invoke(null, new object[] { databaseType })!;
        }

        private static (string Format, string CastTemplate) InvokeThresholdBinding(DatabaseType databaseType)
        {
            var method = typeof(CacheNotifyPollSession).GetMethod(
                "ThresholdBinding", BindingFlags.NonPublic | BindingFlags.Static);
            return ((string, string))method!.Invoke(null, new object[] { databaseType })!;
        }

        [Fact]
        [DisplayName("NaiveNowCommandText 對 SQLite 應回傳 SELECT CURRENT_TIMESTAMP")]
        public void NaiveNowCommandText_SQLite_ReturnsSelectCurrentTimestamp()
        {
            var result = InvokeNaiveNowCommandText(DatabaseType.SQLite);
            Assert.Equal("SELECT CURRENT_TIMESTAMP", result);
        }

        [Fact]
        [DisplayName("NaiveNowCommandText 對未支援的資料庫類型應拋 NotSupportedException")]
        public void NaiveNowCommandText_UnsupportedDatabaseType_ThrowsNotSupportedException()
        {
            var ex = Assert.Throws<TargetInvocationException>(() =>
                InvokeNaiveNowCommandText(s_unsupportedDbType));
            Assert.IsType<NotSupportedException>(ex.InnerException);
        }

        [Fact]
        [DisplayName("ThresholdBinding 對 SQLite 應回傳 'yyyy-MM-dd HH:mm:ss' 格式與 '{0}' cast 模板")]
        public void ThresholdBinding_SQLite_ReturnsExpectedFormatAndCastTemplate()
        {
            var (format, castTemplate) = InvokeThresholdBinding(DatabaseType.SQLite);
            Assert.Equal("yyyy-MM-dd HH:mm:ss", format);
            Assert.Equal("{0}", castTemplate);
        }

        [Fact]
        [DisplayName("ThresholdBinding 對未支援的資料庫類型應拋 NotSupportedException")]
        public void ThresholdBinding_UnsupportedDatabaseType_ThrowsNotSupportedException()
        {
            var ex = Assert.Throws<TargetInvocationException>(() =>
                InvokeThresholdBinding(s_unsupportedDbType));
            Assert.IsType<NotSupportedException>(ex.InnerException);
        }

        [Fact]
        [DisplayName("CacheNotifyPollSession 建構子 marginSeconds 為負數時 _margin 應被 clamp 為 TimeSpan.Zero")]
        public void Constructor_NegativeMarginSeconds_MarginClampedToZero()
        {
            var session = new CacheNotifyPollSession(
                "test_db", new StubDbFactory(), new StubContainer(), new CacheNotifyRouter(), marginSeconds: -5);
            var marginField = typeof(CacheNotifyPollSession).GetField(
                "_margin", BindingFlags.NonPublic | BindingFlags.Instance);
            var margin = (TimeSpan)marginField!.GetValue(session)!;
            Assert.Equal(TimeSpan.Zero, margin);
        }
    }
}
