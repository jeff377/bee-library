using System.ComponentModel;
using System.Reflection;
using Bee.Db.CacheNotify;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// <see cref="CacheNotifyReader"/> 建構子防衛式斷言，以及
    /// <c>NaiveNowCommandText</c> / <c>ThresholdBinding</c> 兩個私有靜態方法
    /// 各資料庫分支與 default 拋例外路徑的單元測試。
    /// </summary>
    public class CacheNotifyReaderUnitTests
    {
        private sealed class StubDbFactory : IDbAccessFactory
        {
            public DbAccess Create(string databaseId) => throw new NotImplementedException();
        }

        private static readonly object[] s_unknownDbTypeArg = [(DatabaseType)999];

        [Fact]
        [DisplayName("CacheNotifyReader 建構子 dbAccessFactory 為 null 應拋 ArgumentNullException")]
        public void Constructor_NullDbAccessFactory_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheNotifyReader(null!));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("ReadBaseline databaseId 為空應拋 ArgumentException")]
        public void ReadBaseline_EmptyDatabaseId_ThrowsArgumentException(string? databaseId)
        {
            var reader = new CacheNotifyReader(new StubDbFactory());
            Assert.ThrowsAny<ArgumentException>(() => reader.ReadBaseline(databaseId!));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [DisplayName("ReadChangesSince databaseId 為空應拋 ArgumentException")]
        public void ReadChangesSince_EmptyDatabaseId_ThrowsArgumentException(string? databaseId)
        {
            var reader = new CacheNotifyReader(new StubDbFactory());
            Assert.ThrowsAny<ArgumentException>(() =>
                reader.ReadChangesSince(databaseId!, DateTime.UnixEpoch));
        }

        // --- NaiveNowCommandText 各 DB 類型分支 ---

        private static MethodInfo GetNaiveNowMethod()
        {
            var method = typeof(CacheNotifyReader).GetMethod(
                "NaiveNowCommandText", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return method!;
        }

        [Theory]
        [InlineData(DatabaseType.SQLServer)]
        [InlineData(DatabaseType.PostgreSQL)]
        [InlineData(DatabaseType.MySQL)]
        [InlineData(DatabaseType.Oracle)]
        [InlineData(DatabaseType.SQLite)]
        [DisplayName("NaiveNowCommandText 已知資料庫類型應回傳非空白 SQL 字串")]
        public void NaiveNowCommandText_KnownDatabaseType_ReturnsNonEmptyString(DatabaseType databaseType)
        {
            var method = GetNaiveNowMethod();
            var result = method.Invoke(null, new object[] { databaseType }) as string;
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        [DisplayName("NaiveNowCommandText 未知資料庫類型應拋 NotSupportedException")]
        public void NaiveNowCommandText_UnknownDatabaseType_ThrowsNotSupportedException()
        {
            var method = GetNaiveNowMethod();
            var ex = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(null, s_unknownDbTypeArg));
            Assert.IsType<NotSupportedException>(ex.InnerException);
        }

        // --- ThresholdBinding 各 DB 類型分支 ---

        private static MethodInfo GetThresholdBindingMethod()
        {
            var method = typeof(CacheNotifyReader).GetMethod(
                "ThresholdBinding", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            return method!;
        }

        [Theory]
        [InlineData(DatabaseType.SQLServer)]
        [InlineData(DatabaseType.PostgreSQL)]
        [InlineData(DatabaseType.MySQL)]
        [InlineData(DatabaseType.Oracle)]
        [InlineData(DatabaseType.SQLite)]
        [DisplayName("ThresholdBinding 已知資料庫類型應回傳非空白 Format 與 CastTemplate")]
        public void ThresholdBinding_KnownDatabaseType_ReturnsBothFieldsNonEmpty(DatabaseType databaseType)
        {
            var method = GetThresholdBindingMethod();
            var result = method.Invoke(null, new object[] { databaseType });
            Assert.NotNull(result);

            var (format, castTemplate) = ((string, string))result!;
            Assert.NotEmpty(format);
            Assert.NotEmpty(castTemplate);
        }

        [Fact]
        [DisplayName("ThresholdBinding 未知資料庫類型應拋 NotSupportedException")]
        public void ThresholdBinding_UnknownDatabaseType_ThrowsNotSupportedException()
        {
            var method = GetThresholdBindingMethod();
            var ex = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(null, s_unknownDbTypeArg));
            Assert.IsType<NotSupportedException>(ex.InnerException);
        }
    }
}
