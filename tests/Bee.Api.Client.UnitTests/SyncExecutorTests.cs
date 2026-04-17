using System.ComponentModel;

namespace Bee.Api.Client.UnitTests
{
    /// <summary>
    /// 針對 <see cref="SyncExecutor"/> 的純邏輯測試。
    /// </summary>
    public class SyncExecutorTests
    {
        [Fact]
        [DisplayName("SyncExecutor.Run(Func<Task>) 應同步執行委派")]
        public void Run_NonGeneric_ExecutesSynchronously()
        {
            bool executed = false;
            SyncExecutor.Run(() =>
            {
                executed = true;
                return Task.CompletedTask;
            });

            Assert.True(executed);
        }

        [Fact]
        [DisplayName("SyncExecutor.Run<T>(Func<Task<T>>) 應回傳非同步結果")]
        public void Run_Generic_ReturnsResult()
        {
            int result = SyncExecutor.Run(() => Task.FromResult(42));
            Assert.Equal(42, result);
        }

        [Fact]
        [DisplayName("SyncExecutor.Run(Func<Task>) 傳入 null 應拋 ArgumentNullException")]
        public void Run_NonGeneric_NullDelegate_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SyncExecutor.Run((Func<Task>)null!));
        }

        [Fact]
        [DisplayName("SyncExecutor.Run<T>(Func<Task<T>>) 傳入 null 應拋 ArgumentNullException")]
        public void Run_Generic_NullDelegate_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SyncExecutor.Run((Func<Task<int>>)null!));
        }

        [Fact]
        [DisplayName("SyncExecutor.Run 委派拋例外時應傳遞原型例外（非 AggregateException 包裝）")]
        public void Run_DelegateThrows_PropagatesException()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => SyncExecutor.Run(() =>
            {
                throw new InvalidOperationException("boom");
            }));
            Assert.Equal("boom", ex.Message);
        }

        [Fact]
        [DisplayName("SyncExecutor.Run<T> 委派拋例外時應傳遞原型例外")]
        public void Run_Generic_DelegateThrows_PropagatesException()
        {
            Assert.Throws<InvalidOperationException>(() => SyncExecutor.Run<int>(() =>
            {
                throw new InvalidOperationException("boom");
            }));
        }

        [Fact]
        [DisplayName("SyncExecutor.Run 應等待非同步委派完成")]
        public void Run_AsyncDelegate_AwaitsCompletion()
        {
            bool completed = false;
            SyncExecutor.Run(async () =>
            {
                await Task.Delay(30).ConfigureAwait(false);
                completed = true;
            });

            Assert.True(completed);
        }
    }
}
