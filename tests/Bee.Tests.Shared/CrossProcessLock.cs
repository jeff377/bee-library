using System.Diagnostics;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// A machine-wide advisory lock held for the lifetime of the instance, used to serialise
    /// setup steps that several test processes would otherwise run concurrently against the
    /// same physical database.
    /// </summary>
    /// <remarks>
    /// Exclusivity comes from opening a lock file with <see cref="FileShare.None"/>. The OS
    /// drops the handle when the owning process exits, so a crashed holder cannot wedge the
    /// lock the way an abandoned named mutex can, and no cleanup step is required. Acquisition
    /// is best-effort: when the timeout elapses the caller proceeds without ownership, because
    /// a fixture that hangs is worse than one that races and every step guarded by this lock
    /// is independently race-tolerant.
    /// </remarks>
    internal sealed class CrossProcessLock : IDisposable
    {
        private static readonly TimeSpan s_pollInterval = TimeSpan.FromMilliseconds(250);

        private readonly FileStream? _stream;

        private CrossProcessLock(FileStream? stream) => _stream = stream;

        /// <summary>
        /// Acquires the machine-wide lock identified by <paramref name="name"/>, waiting up to
        /// <paramref name="timeout"/> for the current holder before giving up and returning an
        /// instance that owns nothing (the caller proceeds unserialised; the give-up is logged).
        /// </summary>
        /// <param name="name">
        /// Lock file name, resolved under the temp directory. Every process guarding the same
        /// resource must pass the same name.
        /// </param>
        /// <param name="timeout">How long to wait for the current holder to finish.</param>
        public static CrossProcessLock Acquire(string name, TimeSpan timeout)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var path = Path.Combine(Path.GetTempPath(), name);
            var waited = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    return new CrossProcessLock(stream);
                }
                catch (IOException)
                {
                    // Contention only. UnauthorizedAccessException is deliberately not caught:
                    // that one says the path is unusable, which no amount of waiting fixes.
                    if (waited.Elapsed >= timeout)
                    {
                        Console.WriteLine(
                            $"CrossProcessLock: '{name}' still held after {timeout.TotalSeconds:F0}s — continuing without it.");
                        return new CrossProcessLock(null);
                    }
                    Thread.Sleep(s_pollInterval);
                }
            }
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        public void Dispose() => _stream?.Dispose();
    }
}
