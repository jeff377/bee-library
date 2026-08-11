using System.Collections.Concurrent;
using Bee.Definition.Security;

namespace Bee.Business.Security
{
    /// <summary>
    /// In-memory login attempt tracker that enforces account lockout after consecutive failed attempts.
    /// </summary>
    /// <remarks>
    /// Default policy: locks the account for <see cref="LockoutDuration"/> after
    /// <see cref="MaxFailedAttempts"/> failures inside <see cref="LockoutDuration"/>.
    /// <para>
    /// WARNING: The key is whatever user id the caller supplied, and <c>System.Login</c> is
    /// anonymous — so every entry in this map is attacker-chosen. Two properties follow from that
    /// and must survive any rewrite:
    /// </para>
    /// <para>
    /// <b>Entries expire on their own.</b> Removal cannot depend on a later call naming the same
    /// user, because an attacker never repeats a user id. Before this was bounded, an entry below
    /// the lockout threshold had no expiry at all and nothing ever removed it: a stream of random
    /// user ids against the anonymous login endpoint grew the map without limit.
    /// </para>
    /// <para>
    /// <b>The failure count is windowed.</b> A count that only ever increased would eventually lock
    /// out a legitimate user for typos spread across months.
    /// </para>
    /// </remarks>
    public class LoginAttemptTracker : ILoginAttemptTracker
    {
        /// <summary>
        /// Default maximum number of consecutive failed attempts before lockout.
        /// </summary>
        public const int DefaultMaxFailedAttempts = 5;

        /// <summary>
        /// Default lockout duration in minutes.
        /// </summary>
        public const int DefaultLockoutMinutes = 15;

        /// <summary>
        /// Default upper bound on the number of accounts tracked at once.
        /// </summary>
        /// <remarks>
        /// Well above any real concurrent-login population, and low enough that the map cannot
        /// become a memory-exhaustion lever for an unauthenticated caller.
        /// </remarks>
        public const int DefaultMaxTrackedAccounts = 10_000;

        private readonly ConcurrentDictionary<string, AttemptInfo> _attempts
            = new ConcurrentDictionary<string, AttemptInfo>(StringComparer.OrdinalIgnoreCase);

        private readonly TimeProvider _timeProvider;
        private readonly Lock _sweepLock = new();
        private DateTime _nextSweepUtc = DateTime.MinValue;

        /// <summary>
        /// Gets the maximum number of consecutive failed attempts before lockout.
        /// </summary>
        public int MaxFailedAttempts { get; }

        /// <summary>
        /// Gets the lockout duration. It is also the counting window and the lifetime of an entry
        /// that has not reached the threshold.
        /// </summary>
        public TimeSpan LockoutDuration { get; }

        /// <summary>
        /// Gets the upper bound on the number of accounts tracked at once.
        /// </summary>
        public int MaxTrackedAccounts { get; init; } = DefaultMaxTrackedAccounts;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginAttemptTracker"/> class with default policy.
        /// </summary>
        public LoginAttemptTracker()
            : this(DefaultMaxFailedAttempts, TimeSpan.FromMinutes(DefaultLockoutMinutes))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginAttemptTracker"/> class with the specified policy.
        /// </summary>
        /// <param name="maxFailedAttempts">The maximum number of consecutive failed attempts before lockout.</param>
        /// <param name="lockoutDuration">The duration of the lockout period.</param>
        public LoginAttemptTracker(int maxFailedAttempts, TimeSpan lockoutDuration)
            : this(maxFailedAttempts, lockoutDuration, TimeProvider.System)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginAttemptTracker"/> class with the specified
        /// policy and time source.
        /// </summary>
        /// <param name="maxFailedAttempts">The maximum number of consecutive failed attempts before lockout.</param>
        /// <param name="lockoutDuration">The duration of the lockout period.</param>
        /// <param name="timeProvider">
        /// The time source used to evaluate lockout expiry. Inject a fake provider in tests so lockout
        /// windows advance by logical time instead of the wall clock.
        /// </param>
        public LoginAttemptTracker(int maxFailedAttempts, TimeSpan lockoutDuration, TimeProvider timeProvider)
        {
            if (maxFailedAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxFailedAttempts), "Must be greater than zero.");
            if (lockoutDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(lockoutDuration), "Must be greater than zero.");
            ArgumentNullException.ThrowIfNull(timeProvider);

            MaxFailedAttempts = maxFailedAttempts;
            LockoutDuration = lockoutDuration;
            _timeProvider = timeProvider;
        }

        /// <inheritdoc />
        public bool IsLockedOut(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            if (!_attempts.TryGetValue(userId, out var info))
                return false;

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            if (info.ExpiresUtc <= now)
            {
                _attempts.TryRemove(new KeyValuePair<string, AttemptInfo>(userId, info));
                return false;
            }

            return info.LockedUntilUtc.HasValue && info.LockedUntilUtc.Value > now;
        }

        /// <inheritdoc />
        public void RecordFailure(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            SweepIfDue(now);

            // The cap applies to *new* accounts only: an account already being tracked must keep
            // accumulating, or a flood of unknown user ids would be a way to switch lockout off for
            // the account the attacker actually wants. New accounts are dropped instead of evicting
            // an existing entry, because eviction would hand back that same lever.
            if (!_attempts.ContainsKey(userId) && _attempts.Count >= MaxTrackedAccounts)
                return;

            _attempts.AddOrUpdate(userId,
                _ => CreateFirstFailure(now),
                (_, existing) => IncrementFailure(existing, now));
        }

        /// <inheritdoc />
        public void Reset(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            _attempts.TryRemove(userId, out _);
        }

        /// <summary>
        /// Drops entries that have outlived their window, at most once per <see cref="LockoutDuration"/>.
        /// </summary>
        /// <remarks>
        /// Lazy per-key cleanup is not enough here. It only runs when someone names the same user
        /// again, and the caller driving the growth never does — every request carries a fresh user
        /// id. The sweep is what makes removal independent of the attacker's choices.
        /// </remarks>
        private void SweepIfDue(DateTime now)
        {
            if (now < _nextSweepUtc)
                return;

            lock (_sweepLock)
            {
                if (now < _nextSweepUtc)
                    return;
                _nextSweepUtc = now.Add(LockoutDuration);
            }

            foreach (var pair in _attempts)
            {
                if (pair.Value.ExpiresUtc <= now)
                    _attempts.TryRemove(pair);
            }
        }

        private AttemptInfo CreateFirstFailure(DateTime now)
        {
            return new AttemptInfo
            {
                FailedCount = 1,
                WindowStartUtc = now,
                LockedUntilUtc = null,
                ExpiresUtc = now.Add(LockoutDuration)
            };
        }

        private AttemptInfo IncrementFailure(AttemptInfo existing, DateTime now)
        {
            // Keep an active lockout as it stands: further failures must not extend it, or an
            // attacker could hold a legitimate account locked indefinitely.
            if (existing.LockedUntilUtc.HasValue && existing.LockedUntilUtc.Value > now)
                return existing;

            // Failures older than one lockout window start the count over, so a legitimate user is
            // not locked out by typos spread across months.
            var withinWindow = now - existing.WindowStartUtc < LockoutDuration;
            var windowStart = withinWindow ? existing.WindowStartUtc : now;
            var newCount = withinWindow ? existing.FailedCount + 1 : 1;

            DateTime? lockedUntil = newCount >= MaxFailedAttempts ? now.Add(LockoutDuration) : null;

            return new AttemptInfo
            {
                FailedCount = newCount,
                WindowStartUtc = windowStart,
                LockedUntilUtc = lockedUntil,
                ExpiresUtc = lockedUntil ?? windowStart.Add(LockoutDuration)
            };
        }

        /// <summary>
        /// Tracks the state of login attempts for a single user.
        /// </summary>
        private sealed class AttemptInfo
        {
            /// <summary>Number of failures inside the current window.</summary>
            public int FailedCount { get; init; }

            /// <summary>When the current counting window opened.</summary>
            public DateTime WindowStartUtc { get; init; }

            /// <summary>When the lockout ends, or <c>null</c> when not locked out.</summary>
            public DateTime? LockedUntilUtc { get; init; }

            /// <summary>When this entry may be dropped, whether or not it is ever named again.</summary>
            public DateTime ExpiresUtc { get; init; }
        }
    }
}
