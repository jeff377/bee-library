using System.Collections.Concurrent;
using System.Diagnostics;
using Bee.LoadTests.Configuration;
using Bee.LoadTests.Statistics;

namespace Bee.LoadTests.Running
{
    /// <summary>
    /// Drives scenarios across a fixed number of virtual users and collects timings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default is a closed model: each virtual user issues its next call only after the
    /// previous one returns. Send rate therefore falls as the system slows, which means this
    /// <b>will not</b> show the tail-latency collapse an open model finds — but it does match how
    /// a person using a business application behaves, waiting for one screen before asking for the
    /// next. Read the numbers knowing which model produced them.
    /// </para>
    /// <para>
    /// The warm-up window runs the same work and throws its samples away. Without it the
    /// percentiles describe cache population rather than steady state.
    /// </para>
    /// </remarks>
    public static class LoadRunner
    {
        /// <summary>
        /// Runs the warm-up window and then the measured window.
        /// </summary>
        /// <param name="load">Load shape: users, durations, model.</param>
        /// <param name="scenarios">Scenarios to drive, already filtered to the enabled ones.</param>
        /// <param name="weights">Weight per scenario name; missing names default to 1.</param>
        /// <param name="onWarmupComplete">
        /// Invoked between the two windows. Use it to reset counters that should describe the
        /// measured window only.
        /// </param>
        /// <param name="cancellationToken">Cancels both windows.</param>
        /// <returns>One result per scenario that was called at least once.</returns>
        public static async Task<IReadOnlyList<ScenarioResult>> RunAsync(
            LoadOptions load,
            IReadOnlyList<IScenario> scenarios,
            IReadOnlyDictionary<string, int> weights,
            Action? onWarmupComplete = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(load);
            ArgumentNullException.ThrowIfNull(scenarios);
            ArgumentNullException.ThrowIfNull(weights);

            if (scenarios.Count == 0)
            {
                throw new ArgumentException("At least one scenario is required.", nameof(scenarios));
            }

            var schedule = BuildSchedule(scenarios, weights);

            if (load.WarmupSeconds > 0)
            {
                await DriveAsync(load, schedule, TimeSpan.FromSeconds(load.WarmupSeconds),
                    collect: false, cancellationToken).ConfigureAwait(false);
            }

            onWarmupComplete?.Invoke();

            return await DriveAsync(load, schedule, TimeSpan.FromSeconds(load.DurationSeconds),
                collect: true, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Expands scenarios into the order virtual users walk, repeating each by its weight.
        /// </summary>
        /// <param name="scenarios">The scenarios.</param>
        /// <param name="weights">Weight per scenario name.</param>
        /// <returns>The expanded schedule.</returns>
        /// <remarks>
        /// Expanding rather than drawing at random keeps the mix exact and repeatable: a run is
        /// meant to be compared against another run, and a sampled mix would differ slightly every
        /// time for no benefit.
        /// </remarks>
        internal static IScenario[] BuildSchedule(
            IReadOnlyList<IScenario> scenarios, IReadOnlyDictionary<string, int> weights)
        {
            var schedule = new List<IScenario>();
            foreach (var scenario in scenarios)
            {
                var weight = weights.TryGetValue(scenario.Name, out var configured) ? configured : 1;
                for (int i = 0; i < Math.Max(weight, 1); i++)
                {
                    schedule.Add(scenario);
                }
            }
            return [.. schedule];
        }

        private static async Task<IReadOnlyList<ScenarioResult>> DriveAsync(
            LoadOptions load,
            IScenario[] schedule,
            TimeSpan window,
            bool collect,
            CancellationToken cancellationToken)
        {
            var samples = new ConcurrentDictionary<string, ConcurrentBag<double>>(StringComparer.Ordinal);
            var errors = new ConcurrentDictionary<string, ConcurrentDictionary<string, long>>(StringComparer.Ordinal);
            var errorSamples = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>(StringComparer.Ordinal);

            using var windowCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            windowCts.CancelAfter(window);
            var token = windowCts.Token;

            var startedAt = Stopwatch.GetTimestamp();

            var workers = new Task[load.VirtualUsers];
            for (int index = 0; index < load.VirtualUsers; index++)
            {
                var virtualUserIndex = index;
                workers[index] = Task.Run(
                    () => WorkerAsync(virtualUserIndex, schedule, samples, errors, errorSamples, collect, token),
                    CancellationToken.None);
            }

            await Task.WhenAll(workers).ConfigureAwait(false);
            var elapsed = Stopwatch.GetElapsedTime(startedAt);

            if (!collect) { return []; }

            var results = new List<ScenarioResult>();
            foreach (var scenario in schedule.DistinctBy(s => s.Name, StringComparer.Ordinal))
            {
                var scenarioSamples = samples.TryGetValue(scenario.Name, out var bag)
                    ? bag.ToArray()
                    : [];
                var scenarioErrors = errors.TryGetValue(scenario.Name, out var map)
                    ? map.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
                    : [];
                var scenarioSampleMessages = errorSamples.TryGetValue(scenario.Name, out var sampleMap)
                    ? sampleMap.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
                    : [];

                if (scenarioSamples.Length == 0 && scenarioErrors.Count == 0) { continue; }

                results.Add(new ScenarioResult(
                    scenario.Name,
                    LatencyStatistics.FromMilliseconds(scenarioSamples),
                    scenarioSamples.Length,
                    scenarioErrors,
                    scenarioSampleMessages,
                    elapsed));
            }
            return results;
        }

        private static async Task WorkerAsync(
            int virtualUserIndex,
            IScenario[] schedule,
            ConcurrentDictionary<string, ConcurrentBag<double>> samples,
            ConcurrentDictionary<string, ConcurrentDictionary<string, long>> errors,
            ConcurrentDictionary<string, ConcurrentDictionary<string, string>> errorSamples,
            bool collect,
            CancellationToken token)
        {
            long iteration = 0;

            while (!token.IsCancellationRequested)
            {
                // Each virtual user starts at a different point in the schedule, so a run does not
                // begin with every user hitting the same scenario at once.
                var scenario = schedule[(int)((virtualUserIndex + iteration) % schedule.Length)];
                var context = new ScenarioContext(virtualUserIndex, iteration);

                var started = Stopwatch.GetTimestamp();
                try
                {
                    await scenario.ExecuteAsync(context, token).ConfigureAwait(false);

                    if (collect)
                    {
                        var elapsed = Stopwatch.GetElapsedTime(started);
                        samples.GetOrAdd(scenario.Name, _ => []).Add(elapsed.TotalMilliseconds);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // The window closed mid-call. That is the expected way a run ends, not a
                    // failure of the scenario, so it is neither timed nor counted as an error.
                    break;
                }
#pragma warning disable CA1031 // A load test must survive any scenario failure and report it;
                              // letting one exception type through would end the whole run.
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    if (collect)
                    {
                        var byType = errors.GetOrAdd(scenario.Name,
                            _ => new ConcurrentDictionary<string, long>(StringComparer.Ordinal));
                        byType.AddOrUpdate(ex.GetType().Name, 1, (_, count) => count + 1);

                        errorSamples
                            .GetOrAdd(scenario.Name,
                                _ => new ConcurrentDictionary<string, string>(StringComparer.Ordinal))
                            .TryAdd(ex.GetType().Name, ex.Message);
                    }
                }

                iteration++;
            }
        }
    }
}
