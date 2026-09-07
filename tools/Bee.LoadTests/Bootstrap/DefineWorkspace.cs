using Bee.Base.Serialization;
using Bee.Definition.Database;
using Bee.Definition.Settings;
using Bee.LoadTests.Configuration;

namespace Bee.LoadTests.Bootstrap
{
    /// <summary>
    /// A private, writable copy of a definition set, adjusted for a load-test run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The definitions are copied rather than used in place for two reasons. Bootstrapping
    /// registers database servers and items through <c>IDefineAccess</c>, which writes back to the
    /// definition directory; and the source set is checked in, so writing to it would leave the
    /// working tree dirty after every run.
    /// </para>
    /// <para>
    /// Three things are overridden on the copy. The database bindings are rewritten for the
    /// configured provider — the shipped ones name SQLite, which is not a load-test target. Debug
    /// mode is turned off. The audit-log background writer is turned back on: the Northwind
    /// definitions disable it so a sign-in record is visible the moment sign-in returns, which is a
    /// demo's choice, and measuring with it off would report the cost of writing an audit row
    /// synchronously on every login rather than what a production host does.
    /// </para>
    /// </remarks>
    public sealed class DefineWorkspace : IDisposable
    {
        private DefineWorkspace(string definePath) => DefinePath = definePath;

        /// <summary>
        /// Gets the path to the writable copy.
        /// </summary>
        public string DefinePath { get; }

        /// <summary>
        /// Copies the definition set and applies the load-test overrides.
        /// </summary>
        /// <param name="sourceDefinePath">The definition directory to copy from.</param>
        /// <param name="options">The run configuration.</param>
        /// <param name="connectionString">
        /// The connection string to bind every category to. Passed in rather than read from the
        /// environment here so the rewriting can be tested without touching process-wide state.
        /// <see cref="ResolveConnectionString"/> is what the driver reads it with.
        /// </param>
        /// <returns>A workspace whose <see cref="DefinePath"/> is ready to bootstrap from.</returns>
        public static DefineWorkspace CreateFrom(
            string sourceDefinePath, LoadTestOptions options, string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceDefinePath);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            if (!Directory.Exists(sourceDefinePath))
            {
                throw new DirectoryNotFoundException(
                    $"Definition directory not found: '{sourceDefinePath}'.");
            }

            var target = Path.Combine(Path.GetTempPath(),
                "bee-loadtest-define-" + Guid.NewGuid().ToString("N"));
            CopyDirectory(sourceDefinePath, target);

            var workspace = new DefineWorkspace(target);
            try
            {
                workspace.OverrideDatabaseSettings(options, connectionString);
                workspace.OverrideSystemSettings();
                return workspace;
            }
            catch
            {
                workspace.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Resolves the connection string for a provider from the environment.
        /// </summary>
        /// <param name="provider">The database engine.</param>
        /// <returns>The connection string.</returns>
        /// <remarks>
        /// The variable name follows the same convention <c>test.sh</c> uses, so a machine already
        /// set up to run the test suite needs no further configuration.
        /// </remarks>
        public static string ResolveConnectionString(DatabaseType provider)
        {
            var variable = GetConnectionStringVariable(provider);
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{variable}' is not set, so there is no {provider} " +
                    "database to measure. Set it the way ./test.sh does, or point the run at a " +
                    "provider whose variable is set.");
            }
            return value;
        }

        /// <summary>
        /// Gets the environment variable name holding a provider's connection string.
        /// </summary>
        /// <param name="provider">The database engine.</param>
        /// <returns>The variable name.</returns>
        public static string GetConnectionStringVariable(DatabaseType provider)
            => "BEE_TEST_CONNSTR_" + provider.ToString().ToUpperInvariant();

        /// <summary>
        /// Deletes the temporary copy.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (Directory.Exists(DefinePath)) { Directory.Delete(DefinePath, recursive: true); }
            }
            catch (IOException)
            {
                // A leftover temp directory is not worth failing a completed run over; the OS
                // reclaims it. Swallowing is deliberate and limited to IO faults.
            }
            catch (UnauthorizedAccessException)
            {
                // As above.
            }
        }

        private void OverrideDatabaseSettings(LoadTestOptions options, string connectionString)
        {
            var path = Path.Combine(DefinePath, "DatabaseSettings.xml");
            var settings = XmlCodec.DeserializeFromFile<DatabaseSettings>(path)
                ?? throw new InvalidOperationException($"Could not read '{path}'.");

            if (settings.Items is null || settings.Items.Count == 0)
            {
                throw new InvalidOperationException(
                    $"'{path}' declares no database items, so there is nothing to point at a " +
                    "load-test database.");
            }

            foreach (var item in settings.Items)
            {
                item.DatabaseType = options.Database.Provider;
                // {@DbName} lets one connection string serve every category, which is how the
                // test harness expresses its connection strings; a string without the placeholder
                // is left as-is and every category shares one database.
                item.ConnectionString = connectionString.Replace(
                    "{@DbName}", item.CategoryId, StringComparison.Ordinal);
                item.DisplayName = $"Load test ({options.Database.Provider}, {item.CategoryId})";
            }

            XmlCodec.SerializeToFile(settings, path);
        }

        private void OverrideSystemSettings()
        {
            var path = Path.Combine(DefinePath, "SystemSettings.xml");
            var settings = XmlCodec.DeserializeFromFile<SystemSettings>(path)
                ?? throw new InvalidOperationException($"Could not read '{path}'.");

            settings.CommonConfiguration.IsDebugMode = false;
            settings.BackendConfiguration.AuditLogOptions.UseBackgroundWriter = true;

            XmlCodec.SerializeToFile(settings, path);
        }

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);

            foreach (var file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
            }

            foreach (var directory in Directory.GetDirectories(source))
            {
                CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
            }
        }
    }
}
