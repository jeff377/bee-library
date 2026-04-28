using System.Collections.Concurrent;
using Bee.Definition.Security;

namespace Bee.Business.Security
{
    /// <summary>
    /// In-memory login attempt tracker that enforces account lockout after consecutive failed attempts.
    /// </summary>
    /// <remarks>
    /// Default policy: locks the account for <see cref="LockoutDuration"/> after <see cref="MaxFailedAttempts"/>
    /// consecutive failures. Expired entries are cleaned up lazily on access.
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

        private readonly ConcurrentDictionary<string, AttemptInfo> _attempts
            = new ConcurrentDictionary<string, AttemptInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the maximum number of consecutive failed attempts before lockout.
        /// </summary>
        public int MaxFailedAttempts { get; }

        /// <summary>
        /// Gets the lockout duration.
        /// </summary>
        public TimeSpan LockoutDuration { get; }

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
        {
            if (maxFailedAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxFailedAttempts), "Must be greater than zero.");
            if (lockoutDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(lockoutDuration), "Must be greater than zero.");

            MaxFailedAttempts = maxFailedAttempts;
            LockoutDuration = lockoutDuration;
        }

        /// <inheritdoc />
        public bool IsLockedOut(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            if (!_attempts.TryGetValue(userId, out var info))
                return false;

            // If lockout has expired, clean up and return false
            if (info.LockedUntilUtc.HasValue && info.LockedUntilUtc.Value <= DateTime.UtcNow)
            {
                _attempts.TryRemove(userId, out _);
                return false;
            }

            return info.LockedUntilUtc.HasValue;
        }

        /// <inheritdoc />
        public void RecordFailure(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            _attempts.AddOrUpdate(userId,
                // Add: first failure
                _ => CreateFirstFailure(),
                // Update: increment failure count
                (_, existing) => IncrementFailure(existing));
        }

        /// <inheritdoc />
        public void Reset(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            _attempts.TryRemove(userId, out _);
        }

        private static AttemptInfo CreateFirstFailure()
        {
            return new AttemptInfo
            {
                FailedCount = 1,
                LockedUntilUtc = null
            };
        }

        private AttemptInfo IncrementFailure(AttemptInfo existing)
        {
            // If currently locked and lockout hasn't expired, keep the lockout
            if (existing.LockedUntilUtc.HasValue && existing.LockedUntilUtc.Value > DateTime.UtcNow)
                return existing;

            int newCount = existing.FailedCount + 1;
            DateTime? lockedUntil = null;

            if (newCount >= MaxFailedAttempts)
            {
                lockedUntil = DateTime.UtcNow.Add(LockoutDuration);
            }

            return new AttemptInfo
            {
                FailedCount = newCount,
                LockedUntilUtc = lockedUntil
            };
        }

        /// <summary>
        /// Tracks the state of login attempts for a single user.
        /// </summary>
        private sealed class AttemptInfo
        {
            public int FailedCount { get; set; }
            public DateTime? LockedUntilUtc { get; set; }
        }
    }
}
