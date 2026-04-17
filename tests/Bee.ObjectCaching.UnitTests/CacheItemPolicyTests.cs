using System.ComponentModel;

namespace Bee.ObjectCaching.UnitTests
{
    public class CacheItemPolicyTests
    {
        [Fact]
        [DisplayName("預設建構子的屬性應為預設值")]
        public void DefaultConstructor_PropertiesAreDefault()
        {
            var policy = new CacheItemPolicy();

            Assert.Equal(DateTimeOffset.MaxValue, policy.AbsoluteExpiration);
            Assert.Equal(TimeSpan.Zero, policy.SlidingExpiration);
            Assert.Null(policy.ChangeMonitorFilePaths);
            Assert.Null(policy.ChangeMonitorDbKeys);
        }

        [Fact]
        [DisplayName("以 SlidingTime 建構應只設定 SlidingExpiration")]
        public void Constructor_SlidingTime_SetsSlidingExpirationOnly()
        {
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 15);

            Assert.Equal(TimeSpan.FromMinutes(15), policy.SlidingExpiration);
            Assert.Equal(DateTimeOffset.MaxValue, policy.AbsoluteExpiration);
        }

        [Fact]
        [DisplayName("以 AbsoluteTime 建構應只設定 AbsoluteExpiration")]
        public void Constructor_AbsoluteTime_SetsAbsoluteExpirationOnly()
        {
            var before = DateTimeOffset.Now.AddMinutes(10);
            var policy = new CacheItemPolicy(CacheTimeKind.AbsoluteTime, 10);
            var after = DateTimeOffset.Now.AddMinutes(10);

            Assert.True(policy.AbsoluteExpiration >= before && policy.AbsoluteExpiration <= after);
            Assert.Equal(TimeSpan.Zero, policy.SlidingExpiration);
        }

        [Fact]
        [DisplayName("可指定 ChangeMonitorFilePaths 與 ChangeMonitorDbKeys")]
        public void ChangeMonitor_Properties_AreAssignable()
        {
            var policy = new CacheItemPolicy
            {
                ChangeMonitorFilePaths = new[] { "a.txt", "b.txt" },
                ChangeMonitorDbKeys = new[] { "key1" }
            };

            Assert.Equal(2, policy.ChangeMonitorFilePaths!.Length);
            Assert.Single(policy.ChangeMonitorDbKeys!);
        }

        [Theory]
        [InlineData(CacheTimeKind.SlidingTime)]
        [InlineData(CacheTimeKind.AbsoluteTime)]
        [DisplayName("CacheTimeKind 列舉值應為定義過的成員")]
        public void CacheTimeKind_DefinedValues_AreValid(CacheTimeKind kind)
        {
            Assert.True(Enum.IsDefined(typeof(CacheTimeKind), kind));
        }
    }
}
