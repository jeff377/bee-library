using System.ComponentModel;
using Bee.Definition.Database;
using Bee.LoadTests.Bootstrap;

namespace Bee.LoadTests.UnitTests
{
    /// <summary>
    /// Tests for <see cref="DbProviderRegistrar"/>.
    /// </summary>
    /// <remarks>
    /// Only the rejection path is covered. Registering a provider mutates the process-wide
    /// <c>DbProviderRegistry</c>, and a test that does so would leak into every other test in the
    /// assembly; the successful path is exercised by actually starting a host instead — which is
    /// also why SQLServer, PostgreSQL, MySQL and Oracle cannot be asserted here now that each has
    /// a driver.
    /// </remarks>
    public class DbProviderRegistrarTests
    {
        [Fact]
        [DisplayName("SQLite 被拒，且訊息說明補救步驟")]
        public void Register_Sqlite_ThrowsWithRemediation()
        {
            // With all four server-side engines now carrying a driver, SQLite is the only value
            // that reaches the rejection path — which is correct: it is not a load-test target.
            // Configuration validation already rejects it earlier; this is the second layer.
            var ex = Assert.Throws<NotSupportedException>(
                () => DbProviderRegistrar.Register(DatabaseType.SQLite));

            Assert.Contains("SQLite", ex.Message, StringComparison.Ordinal);
            Assert.Contains("DbProviderRegistrar", ex.Message, StringComparison.Ordinal);
        }

    }
}
