using System;
using System.ComponentModel;
using Bee.Business.Provider;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// LoginAttemptTracker 暴力破解防護測試
    /// </summary>
    public class LoginAttemptTrackerTests
    {
        [Fact]
        [DisplayName("新帳號不應被鎖定")]
        public void IsLockedOut_NewUser_ReturnsFalse()
        {
            var tracker = new LoginAttemptTracker();
            Assert.False(tracker.IsLockedOut("user01"));
        }

        [Fact]
        [DisplayName("未達最大失敗次數不應被鎖定")]
        public void IsLockedOut_BelowMaxAttempts_ReturnsFalse()
        {
            var tracker = new LoginAttemptTracker(5, TimeSpan.FromMinutes(15));

            for (int i = 0; i < 4; i++)
                tracker.RecordFailure("user01");

            Assert.False(tracker.IsLockedOut("user01"));
        }

        [Fact]
        [DisplayName("達到最大失敗次數應被鎖定")]
        public void IsLockedOut_ReachMaxAttempts_ReturnsTrue()
        {
            var tracker = new LoginAttemptTracker(5, TimeSpan.FromMinutes(15));

            for (int i = 0; i < 5; i++)
                tracker.RecordFailure("user01");

            Assert.True(tracker.IsLockedOut("user01"));
        }

        [Fact]
        [DisplayName("鎖定期間過後應自動解鎖")]
        public void IsLockedOut_AfterLockoutExpires_ReturnsFalse()
        {
            // Use a very short lockout duration for testing
            var tracker = new LoginAttemptTracker(3, TimeSpan.FromMilliseconds(50));

            for (int i = 0; i < 3; i++)
                tracker.RecordFailure("user01");

            Assert.True(tracker.IsLockedOut("user01"));

            // Wait for lockout to expire
            global::System.Threading.Thread.Sleep(100);

            Assert.False(tracker.IsLockedOut("user01"));
        }

        [Fact]
        [DisplayName("成功登入後應重設失敗計數")]
        public void Reset_AfterFailures_ClearsLockout()
        {
            var tracker = new LoginAttemptTracker(3, TimeSpan.FromMinutes(15));

            for (int i = 0; i < 3; i++)
                tracker.RecordFailure("user01");

            Assert.True(tracker.IsLockedOut("user01"));

            tracker.Reset("user01");
            Assert.False(tracker.IsLockedOut("user01"));
        }

        [Fact]
        [DisplayName("不同帳號的失敗計數應獨立")]
        public void RecordFailure_DifferentUsers_IndependentTracking()
        {
            var tracker = new LoginAttemptTracker(3, TimeSpan.FromMinutes(15));

            for (int i = 0; i < 3; i++)
                tracker.RecordFailure("user01");

            tracker.RecordFailure("user02");

            Assert.True(tracker.IsLockedOut("user01"));
            Assert.False(tracker.IsLockedOut("user02"));
        }

        [Fact]
        [DisplayName("Reset 後重新計數，需重新累積才會鎖定")]
        public void Reset_ThenFailAgain_RequiresFullCountToLock()
        {
            var tracker = new LoginAttemptTracker(3, TimeSpan.FromMinutes(15));

            // Fail 2 times, then reset
            tracker.RecordFailure("user01");
            tracker.RecordFailure("user01");
            tracker.Reset("user01");

            // Fail 2 more times — should NOT be locked (count was reset)
            tracker.RecordFailure("user01");
            tracker.RecordFailure("user01");
            Assert.False(tracker.IsLockedOut("user01"));

            // Third failure triggers lockout again
            tracker.RecordFailure("user01");
            Assert.True(tracker.IsLockedOut("user01"));
        }

        [Fact]
        [DisplayName("帳號名稱不區分大小寫")]
        public void RecordFailure_CaseInsensitive_TracksAsSameUser()
        {
            var tracker = new LoginAttemptTracker(3, TimeSpan.FromMinutes(15));

            tracker.RecordFailure("User01");
            tracker.RecordFailure("USER01");
            tracker.RecordFailure("user01");

            Assert.True(tracker.IsLockedOut("user01"));
        }

        [Fact]
        [DisplayName("空字串或 null 的 userId 不應拋出例外")]
        public void RecordFailure_NullOrEmpty_DoesNotThrow()
        {
            var tracker = new LoginAttemptTracker();

            tracker.RecordFailure(null!);
            tracker.RecordFailure(string.Empty);
            Assert.False(tracker.IsLockedOut(null!));
            Assert.False(tracker.IsLockedOut(string.Empty));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [DisplayName("MaxFailedAttempts 不可為零或負數")]
        public void Constructor_InvalidMaxAttempts_ThrowsArgumentOutOfRangeException(int maxAttempts)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new LoginAttemptTracker(maxAttempts, TimeSpan.FromMinutes(15)));
        }

        [Fact]
        [DisplayName("LockoutDuration 不可為零或負數")]
        public void Constructor_InvalidDuration_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new LoginAttemptTracker(5, TimeSpan.Zero));
        }
    }
}
