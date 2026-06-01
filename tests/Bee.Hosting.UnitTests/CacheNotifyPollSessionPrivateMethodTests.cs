using System.ComponentModel;
using System.Reflection;
using Bee.Definition.Database;
using Bee.Hosting.CacheNotify;

namespace Bee.Hosting.UnitTests
{
    /// <summary>
    /// 補強 <see cref="CacheNotifyPollSession"/> 兩個 private static 方法的覆蓋率：
    /// <c>NaiveNowCommandText</c> 與 <c>ThresholdBinding</c>。
    /// 已有 DB 整合測試覆蓋 SQL Server / PostgreSQL / MySQL / Oracle 四個分支；
    /// 本類別補 SQLite 分支與不支援型別的 NotSupportedException。
    /// </summary>
    public class CacheNotifyPollSessionPrivateMethodTests
    {
        private static readonly MethodInfo s_naiveNowMethod =
            typeof(CacheNotifyPollSession).GetMethod(
                "NaiveNowCommandText",
                BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly MethodInfo s_thresholdBindingMethod =
            typeof(CacheNotifyPollSession).GetMethod(
                "ThresholdBinding",
                BindingFlags.NonPublic | BindingFlags.Static)!;

        private static readonly object[] s_sqliteArg = { DatabaseType.SQLite };
        private static readonly object[] s_unsupportedDbArg = { (DatabaseType)99 };

        [Fact]
        [DisplayName("NaiveNowCommandText SQLite 應回傳 SELECT CURRENT_TIMESTAMP")]
        public void NaiveNowCommandText_SQLite_ReturnsCurrentTimestampQuery()
        {
            var result = (string)s_naiveNowMethod.Invoke(null, s_sqliteArg)!;
            Assert.Equal("SELECT CURRENT_TIMESTAMP", result);
        }

        [Fact]
        [DisplayName("NaiveNowCommandText 不支援的 DatabaseType 應拋 NotSupportedException")]
        public void NaiveNowCommandText_UnsupportedDatabaseType_ThrowsNotSupportedException()
        {
            var ex = Assert.Throws<TargetInvocationException>(
                () => s_naiveNowMethod.Invoke(null, s_unsupportedDbArg));
            Assert.IsType<NotSupportedException>(ex.InnerException);
        }

        [Fact]
        [DisplayName("ThresholdBinding SQLite 應回傳 yyyy-MM-dd HH:mm:ss 格式與 {0} 範本")]
        public void ThresholdBinding_SQLite_ReturnsCorrectFormatAndCastTemplate()
        {
            var result = s_thresholdBindingMethod.Invoke(null, s_sqliteArg);
            var (format, castTemplate) = ((string, string))result!;
            Assert.Equal("yyyy-MM-dd HH:mm:ss", format);
            Assert.Equal("{0}", castTemplate);
        }

        [Fact]
        [DisplayName("ThresholdBinding 不支援的 DatabaseType 應拋 NotSupportedException")]
        public void ThresholdBinding_UnsupportedDatabaseType_ThrowsNotSupportedException()
        {
            var ex = Assert.Throws<TargetInvocationException>(
                () => s_thresholdBindingMethod.Invoke(null, s_unsupportedDbArg));
            Assert.IsType<NotSupportedException>(ex.InnerException);
        }
    }
}
