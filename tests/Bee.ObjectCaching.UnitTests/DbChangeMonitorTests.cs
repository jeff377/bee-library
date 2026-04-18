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

        [Fact]
        [DisplayName("Timer Elapsed 時 UpdateTime 不同於 GetUpdateTime 應通知 OnChanged")]
        public void TimerElapsed_WhenUpdateTimeDiffers_TriggersOnChanged()
        {
            // GetUpdateTime() 目前回傳 DateTime.MinValue;將 UpdateTime 改為現在時間後,
            // Timer 觸發時會進入 if 分支呼叫 OnChanged,並把 UpdateTime 更新回 MinValue。
            using var monitor = new DbChangeMonitor("change-test");
            monitor.Timer!.Interval = 50;
            monitor.UpdateTime = DateTime.UtcNow;

            var triggered = SpinWait.SpinUntil(() => monitor.HasChanged, 2000);

            Assert.True(triggered, "Monitor did not mark HasChanged within 2s.");
            Assert.Equal(DateTime.MinValue, monitor.UpdateTime);
        }
    }
}
