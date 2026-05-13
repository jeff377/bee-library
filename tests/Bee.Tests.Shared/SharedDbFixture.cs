namespace Bee.Tests.Shared
{
    /// <summary>
    /// Per-class fixture that opts into <see cref="BeeTestFixtureBuilder.UseSharedDatabases"/>:
    /// drives <see cref="SharedDatabaseState.EnsureSchemaAndSeed"/> once-per-process for every
    /// database type whose <c>BEE_TEST_CONNSTR_*</c> env var is set, ensuring the
    /// <c>st_user</c> / <c>st_session</c> schemas + seed user exist before any <c>[DbFact]</c>
    /// / <c>[DbTheory]</c> in the consuming class runs.
    /// </summary>
    /// <remarks>
    /// Use as <c>IClassFixture&lt;SharedDbFixture&gt;</c> for any test class whose tests touch
    /// the shared databases (e.g. via <c>SystemApiConnector.CreateSession</c>). Replaces the
    /// legacy <c>[Collection("Initialize")]</c> + <c>DbGlobalFixture</c> binding for DB tests.
    /// </remarks>
    public sealed class SharedDbFixture : BeeTestFixture
    {
        /// <summary>
        /// Creates a fixture pointing at the shared <c>tests/Define</c> directory with
        /// <see cref="BeeTestFixtureBuilder.UseSharedDatabases"/> enabled.
        /// </summary>
        public SharedDbFixture() : base(b => b.UseSharedDatabases()) { }
    }
}
