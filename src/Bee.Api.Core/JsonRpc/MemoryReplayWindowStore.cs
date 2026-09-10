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
        private readonly TimeProvider _clock;
        private long _lastSweepAtTimestamp;
        private int _sweeping;

        /// <summary>
        /// Initializes a new instance that measures idle time against the system clock.
        /// </summary>
        public MemoryReplayWindowStore() : this(TimeProvider.System) { }

        /// <summary>
        /// Initializes a new instance that measures idle time against <paramref name="clock"/>.
        /// </summary>
        /// <param name="clock">Supplies the timestamps idle time is measured with.</param>
        /// <remarks>
        /// Internal rather than public because the seam exists so tests can move time without
        /// waiting for it. A deployment that wants different eviction supplies its own
        /// <see cref="IReplayWindowStore"/> rather than reclocking this one.
        /// <para>
        /// Idle time is measured with <see cref="TimeProvider.GetTimestamp"/> and never with
        /// <see cref="TimeProvider.GetUtcNow"/>. The former is monotonic — the system provider
        /// reads the same source as <see cref="System.Diagnostics.Stopwatch"/> — so a clock
        /// adjustment or an NTP correction cannot make a window that is in active use suddenly
        /// look idle for hours.
        /// </para>
        /// </remarks>
        internal MemoryReplayWindowStore(TimeProvider clock)
        {
            ArgumentNullException.ThrowIfNull(clock);

            _clock = clock;

            // Start the throttle as though a sweep had just run. The first access used to sweep
            // unconditionally because the field began at zero, but that sweep only ever saw an
            // empty dictionary, so this is equivalent and avoids a sentinel value that a
            // timestamp reading cannot safely reserve.
            _lastSweepAtTimestamp = clock.GetTimestamp();
        }

        /// <inheritdoc />
        public ReplayWindow GetOrAdd(Guid accessToken)
        {
            SweepIfDue();

            long now = _clock.GetTimestamp();
            var entry = _entries.GetOrAdd(accessToken, static (_, timestamp) => new Entry(timestamp), now);
            Volatile.Write(ref entry.LastTouchedTimestamp, now);
            return entry.Window;
        }

        /// <summary>Gets the number of windows currently held; intended for tests and diagnostics.</summary>
        public int Count => _entries.Count;

        private static TimeSpan Lifetime => ApiServiceOptions.WireFrameTimestampTolerance * 2;

        private void SweepIfDue()
        {
            long now = _clock.GetTimestamp();

            // Read once: the tolerance behind it is a mutable static, and a sweep that used one
            // value to decide it was due and another to decide what is stale would be answering
            // two different questions.
            var lifetime = Lifetime;

            if (_clock.GetElapsedTime(Volatile.Read(ref _lastSweepAtTimestamp), now) < lifetime) { return; }

            // One sweeper at a time; everyone else carries on rather than queueing behind it.
            if (Interlocked.Exchange(ref _sweeping, 1) == 1) { return; }
            try
            {
                Volatile.Write(ref _lastSweepAtTimestamp, now);

                foreach (var pair in _entries)
                {
                    // Strictly greater, so an entry idle for exactly the lifetime is kept.
                    if (_clock.GetElapsedTime(Volatile.Read(ref pair.Value.LastTouchedTimestamp), now) > lifetime)
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

        private sealed class Entry(long touchedAt)
        {
            public ReplayWindow Window { get; } = new();

            /// <summary>
            /// When the entry was last used, as a <see cref="TimeProvider.GetTimestamp"/> reading.
            /// </summary>
            /// <remarks>
            /// WARNING: this must never start at its default. <see cref="GetOrAdd"/> writes the
            /// current timestamp only after the entry is in the dictionary, and a sweep running in
            /// that gap would read zero, find it older than any cutoff, and drop a window that is
            /// in active use. The session's next request would then get a fresh window with no
            /// history — which is to say the replay protection would silently reset itself for
            /// that session. Taking the timestamp as a constructor parameter is what closes it:
            /// there is no parameterless form, so the compiler now enforces what a single line of
            /// code used to.
            /// </remarks>
            public long LastTouchedTimestamp = touchedAt;
        }
    }
}
