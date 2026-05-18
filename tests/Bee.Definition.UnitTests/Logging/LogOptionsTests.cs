using System.ComponentModel;
using Bee.Definition.Database;
using Bee.Definition.Logging;

namespace Bee.Definition.UnitTests.Logging
{
    public class LogOptionsTests
    {
        [Fact]
        [DisplayName("LogOptions.ToString 應回傳類別名稱")]
        public void ToString_DefaultInstance_ReturnsTypeName()
        {
            var options = new LogOptions();
            Assert.Equal("LogOptions", options.ToString());
        }

        [Fact]
        [DisplayName("LogOptions 預設建構子應初始化 DbAccess 子選項")]
        public void DefaultConstructor_InitializesDbAccess()
        {
            var options = new LogOptions();
            Assert.NotNull(options.DbAccess);
        }

        [Fact]
        [DisplayName("DbAccessAnomalyLogOptions.ToString 應回傳類別名稱")]
        public void DbAccessAnomalyLogOptions_ToString_ReturnsTypeName()
        {
            var options = new DbAccessAnomalyLogOptions();
            Assert.Equal("DbAccessAnomalyLogOptions", options.ToString());
        }

        [Fact]
        [DisplayName("DbAccessAnomalyLogOptions 預設屬性值應與規格一致")]
        public void DbAccessAnomalyLogOptions_DefaultValues_MatchSpecification()
        {
            var options = new DbAccessAnomalyLogOptions();
            Assert.Equal(DbAccessAnomalyLogLevel.Warning, options.Level);
            Assert.Equal(10000, options.AffectedRowThreshold);
            Assert.Equal(10000, options.ResultRowThreshold);
            Assert.Equal(300, options.ExecutionTimeThreshold);
        }
    }
}
