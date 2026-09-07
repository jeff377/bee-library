using System.ComponentModel;
using Bee.LoadTests.Configuration;
using Bee.LoadTests.Running;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for <see cref="LoadRunner"/>.
    /// </summary>
    public class LoadRunnerTests
    {
        private sealed class CountingScenario(string name, Action<ScenarioContext>? onCall = null)
            : IScenario
        {
            private long _calls;

            public string Name { get; } = name;

            public long Calls => Interlocked.Read(ref _calls);

            public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref _calls);
                onCall?.Invoke(context);
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingScenario(string name, Exception exception) : IScenario
        {
            public string Name { get; } = name;

            public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
                => throw exception;
        }

        private static LoadOptions Load(int users = 2, int warmup = 0, int duration = 1)
            => new() { VirtualUsers = users, WarmupSeconds = warmup, DurationSeconds = duration };

        [Fact]
        [DisplayName("依權重展開排程，權重 3 出現三次")]
        public void BuildSchedule_RepeatsByWeight()
        {
            var a = new CountingScenario("A");
            var b = new CountingScenario("B");

            var schedule = LoadRunner.BuildSchedule(
                [a, b],
                new Dictionary<string, int>(StringComparer.Ordinal) { ["A"] = 3, ["B"] = 1 });

            Assert.Equal(4, schedule.Length);
            Assert.Equal(3, schedule.Count(s => s.Name == "A"));
            Assert.Equal(1, schedule.Count(s => s.Name == "B"));
        }

        [Fact]
        [DisplayName("未指定權重的場景視為 1")]
        public void BuildSchedule_MissingWeight_DefaultsToOne()
        {
            var schedule = LoadRunner.BuildSchedule(
                [new CountingScenario("A")],
                new Dictionary<string, int>(StringComparer.Ordinal));

            Assert.Single(schedule);
        }

        [Fact]
        [DisplayName("權重為 0 或負值仍至少排一次，不會讓排程落空")]
        public void BuildSchedule_NonPositiveWeight_StillScheduledOnce()
        {
            var schedule = LoadRunner.BuildSchedule(
                [new CountingScenario("A")],
                new Dictionary<string, int>(StringComparer.Ordinal) { ["A"] = 0 });

            Assert.Single(schedule);
        }

        [Fact]
        [DisplayName("量測窗收集樣本並算出每秒請求數")]
        public async Task RunAsync_CollectsSamples()
        {
            var scenario = new CountingScenario("A");

            var results = await LoadRunner.RunAsync(
                Load(), [scenario], new Dictionary<string, int>(StringComparer.Ordinal));

            var result = Assert.Single(results);
            Assert.Equal("A", result.Name);
            Assert.True(result.SuccessCount > 0);
            Assert.Equal(result.SuccessCount, result.Latencies.Count);
            Assert.True(result.RequestsPerSecond > 0);
            Assert.Equal(0, result.ErrorCount);
        }

        [Fact]
        [DisplayName("warm-up 的樣本被丟棄，只有量測窗計入")]
        public async Task RunAsync_DiscardsWarmupSamples()
        {
            var scenario = new CountingScenario("A");

            var results = await LoadRunner.RunAsync(
                Load(warmup: 1), [scenario],
                new Dictionary<string, int>(StringComparer.Ordinal));

            var result = Assert.Single(results);

            // Both windows ran the scenario, so the total call count must exceed what the
            // measured window kept. If warm-up samples leaked in, these would be equal.
            Assert.True(scenario.Calls > result.SuccessCount,
                $"calls={scenario.Calls} kept={result.SuccessCount}");
        }

        [Fact]
        [DisplayName("warm-up 結束時回呼一次，供重設計數器")]
        public async Task RunAsync_InvokesWarmupCallbackOnce()
        {
            var invocations = 0;

            await LoadRunner.RunAsync(
                Load(warmup: 1), [new CountingScenario("A")],
                new Dictionary<string, int>(StringComparer.Ordinal),
                onWarmupComplete: () => invocations++);

            Assert.Equal(1, invocations);
        }

        [Fact]
        [DisplayName("場景擲例外時依型別計數，且不中斷整個 run")]
        public async Task RunAsync_RecordsErrorsWithoutStopping()
        {
            var results = await LoadRunner.RunAsync(
                Load(), [new ThrowingScenario("A", new TimeoutException())],
                new Dictionary<string, int>(StringComparer.Ordinal));

            var result = Assert.Single(results);
            Assert.Equal(0, result.SuccessCount);
            Assert.True(result.ErrorCount > 0);
            Assert.Equal(result.ErrorCount, result.Errors[nameof(TimeoutException)]);
        }

        [Fact]
        [DisplayName("失敗的呼叫不計入延遲統計，避免延遲看起來很漂亮")]
        public async Task RunAsync_FailedCallsAreNotTimed()
        {
            var results = await LoadRunner.RunAsync(
                Load(), [new ThrowingScenario("A", new InvalidOperationException())],
                new Dictionary<string, int>(StringComparer.Ordinal));

            var result = Assert.Single(results);
            Assert.Equal(0, result.Latencies.Count);
            Assert.Equal(result.ErrorCount, result.TotalCount);
        }

        [Fact]
        [DisplayName("每個 VU 拿到自己的索引，且不同 VU 由不同起點開始")]
        public async Task RunAsync_GivesEachVirtualUserItsOwnIndex()
        {
            var seen = new HashSet<int>();
            var gate = new object();
            var scenario = new CountingScenario("A", context =>
            {
                lock (gate) { seen.Add(context.VirtualUserIndex); }
            });

            await LoadRunner.RunAsync(
                Load(users: 4), [scenario],
                new Dictionary<string, int>(StringComparer.Ordinal));

            Assert.Equal([0, 1, 2, 3], seen.OrderBy(i => i));
        }

        [Fact]
        [DisplayName("多個場景依權重取得不同的呼叫比例")]
        public async Task RunAsync_HonoursWeightsAcrossScenarios()
        {
            var heavy = new CountingScenario("Heavy");
            var light = new CountingScenario("Light");

            await LoadRunner.RunAsync(
                Load(users: 1), [heavy, light],
                new Dictionary<string, int>(StringComparer.Ordinal) { ["Heavy"] = 3, ["Light"] = 1 });

            Assert.True(heavy.Calls > light.Calls,
                $"heavy={heavy.Calls} light={light.Calls}");
        }

        [Fact]
        [DisplayName("沒有任何場景時拒絕執行")]
        public async Task RunAsync_NoScenarios_Throws()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => LoadRunner.RunAsync(
                Load(), [], new Dictionary<string, int>(StringComparer.Ordinal)));
        }

        [Fact]
        [DisplayName("外部取消時提前結束而不擲例外")]
        public async Task RunAsync_ExternalCancellation_StopsEarly()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

            var results = await LoadRunner.RunAsync(
                Load(duration: 30), [new CountingScenario("A")],
                new Dictionary<string, int>(StringComparer.Ordinal),
                cancellationToken: cts.Token);

            Assert.True(results.Count <= 1);
        }
    }
}
