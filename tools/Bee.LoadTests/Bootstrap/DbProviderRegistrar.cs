using Bee.Db.Manager;
using Bee.Db.Providers.SqlServer;
using Bee.Definition.Database;

namespace Bee.LoadTests.Bootstrap
{
    /// <summary>
    /// Registers the ADO.NET provider factory and SQL dialect for the engine under test.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Bee.Db</c> references no ADO.NET driver: which drivers exist is the host's decision, and
    /// the load-test driver is a host. Only the engines whose driver this project references can be
    /// registered, so adding one is a two-step change — a package reference plus a case below.
    /// </para>
    /// <para>
    /// SQLite is absent on purpose rather than by omission: it is not a load-test target, and
    /// <see cref="Configuration.LoadTestOptions.Validate"/> rejects it before this is reached.
    /// </para>
    /// </remarks>
    public static class DbProviderRegistrar
    {
        /// <summary>
        /// Registers the provider factory and dialect for the given engine.
        /// </summary>
        /// <param name="provider">The database engine.</param>
        /// <exception cref="NotSupportedException">
        /// No driver for that engine is referenced by this project.
        /// </exception>
        public static void Register(DatabaseType provider)
        {
            switch (provider)
            {
                case DatabaseType.SQLServer:
                    DbProviderRegistry.Register(
                        DatabaseType.SQLServer, Microsoft.Data.SqlClient.SqlClientFactory.Instance);
                    DbDialectRegistry.Register(DatabaseType.SQLServer, new SqlDialectFactory());
                    break;

                default:
                    throw new NotSupportedException(
                        $"No ADO.NET driver for {provider} is referenced by Bee.LoadTests. Add the " +
                        "driver package to Bee.LoadTests.csproj and a case to DbProviderRegistrar, " +
                        "or run against a provider that is already wired up.");
            }
        }
    }
}
