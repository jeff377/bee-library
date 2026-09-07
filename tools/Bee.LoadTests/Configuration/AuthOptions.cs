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
    }
}
