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
    /// assembly; the successful path is exercised by actually starting a host instead.
    /// </remarks>
    public class DbProviderRegistrarTests
    {
        [Theory]
        [InlineData(DatabaseType.PostgreSQL)]
        [InlineData(DatabaseType.MySQL)]
        [InlineData(DatabaseType.Oracle)]
        [DisplayName("未引用 driver 的 provider 擲出說明補救步驟的例外")]
        public void Register_ProviderWithoutDriver_ThrowsWithRemediation(DatabaseType provider)
        {
            var ex = Assert.Throws<NotSupportedException>(() => DbProviderRegistrar.Register(provider));

            Assert.Contains(provider.ToString(), ex.Message, StringComparison.Ordinal);
            Assert.Contains("DbProviderRegistrar", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        [DisplayName("SQLite 同樣被拒，且在設定驗證階段就已擋下")]
        public void Register_Sqlite_Throws()
        {
            Assert.Throws<NotSupportedException>(
                () => DbProviderRegistrar.Register(DatabaseType.SQLite));
        }
    }
}
