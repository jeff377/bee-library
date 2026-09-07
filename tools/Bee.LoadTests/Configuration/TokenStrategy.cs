namespace Bee.LoadTests.Configuration
{
    /// <summary>
    /// How virtual users obtain an access token.
    /// </summary>
    public enum TokenStrategy
    {
        /// <summary>
        /// Every virtual user signs in as its own account. This is the multi-user shape, and the
        /// only one that exercises session lookup and the session table under load.
        /// </summary>
        PerUser = 0,

        /// <summary>
        /// All virtual users share one token, which measures a single user hammering the server.
        /// Useful as a contrast run to isolate what session lookup costs.
        /// </summary>
        Shared = 1
    }
}
