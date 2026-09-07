namespace Bee.LoadTests.Configuration
{
    /// <summary>
    /// How virtual users authenticate.
    /// </summary>
    public sealed class AuthOptions
    {
        /// <summary>
        /// Gets or sets whether each virtual user signs in separately or they share one token.
        /// </summary>
        public TokenStrategy TokenStrategy { get; set; } = TokenStrategy.PerUser;

        /// <summary>
        /// Gets or sets how many accounts to seed for <see cref="TokenStrategy.PerUser"/>.
        /// </summary>
        public int UserPoolSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the prefix for seeded account ids; the virtual user index is appended.
        /// </summary>
        public string UserIdPrefix { get; set; } = "loadtest_user_";

        /// <summary>
        /// Gets or sets the password every seeded account signs in with.
        /// </summary>
        /// <remarks>
        /// It lives in configuration rather than in code because seeding and signing in must agree
        /// on it across separate runs, and a value generated per run would stop matching the hash
        /// already stored. These accounts exist only in the load test's own databases — the ones
        /// named by <see cref="DatabaseOptions.DatabaseNamePrefix"/> — and grant nothing anywhere
        /// else. Do not point a run at a database that holds real accounts.
        /// </remarks>
        public string Password { get; set; } = "LoadTest#2026";

        /// <summary>
        /// Gets or sets the company id the seeded accounts are granted access to.
        /// </summary>
        public string CompanyId { get; set; } = "loadtest";
    }
}
