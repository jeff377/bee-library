using System.ComponentModel;
using System.Reflection;
using Bee.Db.CacheNotify;
using Bee.Definition.Database;

namespace Bee.Db.UnitTests
{
    /// <summary>
    /// <see cref="CacheNotifyReader"/> 建構子防衛式斷言，以及
    /// <see cref="CacheNotifyReader.BaselineNowCommandText"/> / <c>ThresholdBinding</c>
    /// 各資料庫分支與 default 拋例外路徑的單元測試。
    /// </summary>
    /// <remarks>
    /// NOTE: baseline 那半改為**直接呼叫** internal 方法（`InternalsVisibleTo` 已開），不再走反射。
    /// 反射版把改名變成執行期的 <c>Assert.NotNull() Failure: Value is null</c> —— 訊息完全不指向
    /// 真因，而編譯器本來可以當場擋下。<c>ThresholdBinding</c> 仍是 private，維持反射。
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

        // --- BaselineNowCommandText 各 DB 類型分支 ---

        // NOTE: 「已知方言回傳非空字串」不放這裡 —— 它現在會查 DbDialectRegistry，而 registry 由
        // SharedDatabaseState 註冊，本類別沒有 fixture，先跑就會拿到空的 registry（順序相依，
        // 表現為間歇性紅）。改由 CacheNotifyBaselineBasisTests 涵蓋，那裡有 fixture，
        // 而且驗的是更強的條件：表達式必須與寫入端完全相同。

        [Fact]
        [DisplayName("BaselineNowCommandText 未知資料庫類型應拋 NotSupportedException")]
        public void BaselineNowCommandText_UnknownDatabaseType_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(
                () => CacheNotifyReader.BaselineNowCommandText((DatabaseType)999));
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
