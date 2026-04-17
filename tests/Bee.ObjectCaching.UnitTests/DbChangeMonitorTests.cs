using System.ComponentModel;

namespace Bee.ObjectCaching.UnitTests
{
    public class DbChangeMonitorTests
    {
        [Fact]
        [DisplayName("DbChangeMonitor 建構後應暴露指定 Key 與唯一識別")]
        public void Constructor_AssignsKeyAndUniqueId()
        {
            using var monitor = new DbChangeMonitor("orgs");

            Assert.Equal("orgs", monitor.Key);
            Assert.False(string.IsNullOrEmpty(monitor.UniqueId));
            Assert.NotNull(monitor.Timer);
            Assert.True(monitor.Timer!.Enabled);
        }

        [Fact]
        [DisplayName("Dispose 後 Timer 應被釋放")]
        public void Dispose_StopsAndReleasesTimer()
        {
            var monitor = new DbChangeMonitor("dispose-target");
            Assert.NotNull(monitor.Timer);

            monitor.Dispose();

            Assert.Null(monitor.Timer);
        }

        [Fact]
        [DisplayName("UpdateTime 應可被指定為任意時間")]
        public void UpdateTime_CanBeAssigned()
        {
            using var monitor = new DbChangeMonitor("update-time");
            var moment = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            monitor.UpdateTime = moment;

            Assert.Equal(moment, monitor.UpdateTime);
        }
    }
}
