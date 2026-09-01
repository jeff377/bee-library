using System.Collections.Concurrent;

namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// The default <see cref="IReplayWindowStore"/>: windows live in this process's memory and are
    /// dropped once a session has been idle longer than the replay window can matter.
    /// </summary>
    /// <remarks>
    /// Entries expire on their own rather than being removed at sign-out, and the lifetime falls
    /// out of the design rather than being a tuning knob: a replay carrying an old sequence number
    /// also carries an old timestamp, so it has already been refused by the timestamp check. A
    /// window is therefore only useful for as long as a timestamp stays acceptable, and the store
    /// keeps entries for twice
    /// <see cref="ApiServiceOptions.WireFrameTimestampTolerance"/> purely as margin.
    /// <para>
    /// That bounds memory at "sessions active within the last few minutes" and, more usefully,
    /// decouples the store from session lifetime entirely — nothing has to remember to evict.
    /// </para>
    /// </remarks>
    public sealed class MemoryReplayWindowStore : IReplayWindowStore
    {
        private readonly ConcurrentDictionary<Guid, Entry> _entries = new();
        private long _nextSweepAtMs;
        private int _sweeping;

        /// <inheritdoc />
        public ReplayWindow GetOrAdd(Guid accessToken)
        {
            SweepIfDue();

            var entry = _entries.GetOrAdd(accessToken, static _ => new Entry());
            Volatile.Write(ref entry.LastTouchedMs, Environment.TickCount64);
            return entry.Window;
        }

        /// <summary>Gets the number of windows currently held; intended for tests and diagnostics.</summary>
        public int Count => _entries.Count;

        private static long LifetimeMs =>
            (long)ApiServiceOptions.WireFrameTimestampTolerance.TotalMilliseconds * 2;

        private void SweepIfDue()
        {
            long now = Environment.TickCount64;
            if (now < Volatile.Read(ref _nextSweepAtMs)) { return; }

            // One sweeper at a time; everyone else carries on rather than queueing behind it.
            if (Interlocked.Exchange(ref _sweeping, 1) == 1) { return; }
            try
            {
                Volatile.Write(ref _nextSweepAtMs, now + LifetimeMs);
                long cutoff = now - LifetimeMs;

                foreach (var pair in _entries)
                {
                    if (Volatile.Read(ref pair.Value.LastTouchedMs) < cutoff)
                    {
                        _entries.TryRemove(pair);
                    }
                }
            }
            finally
            {
                Volatile.Write(ref _sweeping, 0);
            }
        }

        private sealed class Entry
        {
            public ReplayWindow Window { get; } = new();

            public long LastTouchedMs;
        }
    }
}
