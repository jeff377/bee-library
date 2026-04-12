namespace Bee.Definition.Security
{
    /// <summary>
    /// Tracks login attempts and enforces account lockout policies to mitigate brute-force attacks.
    /// </summary>
    public interface ILoginAttemptTracker
    {
        /// <summary>
        /// Determines whether the specified user account is currently locked out due to excessive failed login attempts.
        /// </summary>
        /// <param name="userId">The user account identifier.</param>
        /// <returns><c>true</c> if the account is locked out; otherwise, <c>false</c>.</returns>
        bool IsLockedOut(string userId);

        /// <summary>
        /// Records a failed login attempt for the specified user account.
        /// </summary>
        /// <param name="userId">The user account identifier.</param>
        void RecordFailure(string userId);

        /// <summary>
        /// Resets the failed login attempt counter for the specified user account (e.g., after a successful login).
        /// </summary>
        /// <param name="userId">The user account identifier.</param>
        void Reset(string userId);
    }
}
