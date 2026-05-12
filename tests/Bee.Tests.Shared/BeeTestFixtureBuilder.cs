using Bee.Api.AspNetCore;
using Bee.Definition;
using Bee.Definition.Security;
using Bee.Definition.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Builder for <see cref="BeeTestFixture"/>. Customises the <see cref="PathOptions"/>
    /// resolution and the underlying <see cref="BackendConfiguration"/> before the per-fixture
    /// <see cref="IServiceProvider"/> is built.
    /// </summary>
    public sealed class BeeTestFixtureBuilder
    {
        private bool _useTempDefinePath;
        private Action<BackendConfiguration>? _configureBackend;

        /// <summary>
        /// Redirects the fixture's <see cref="PathOptions.DefinePath"/> to a per-fixture
        /// temporary directory; the directory is seeded with a copy of the shared
        /// <c>tests/Define</c> fixture (so <c>SystemSettings.xml</c> etc. resolve correctly)
        /// and deleted when the fixture is disposed.
        /// </summary>
        public BeeTestFixtureBuilder UseTempDefinePath()
        {
            _useTempDefinePath = true;
            return this;
        }

        /// <summary>
        /// Applies a callback to the loaded <see cref="BackendConfiguration"/> before the
        /// service provider is built. Useful for switching encryption-key sources, swapping
        /// component types, etc.
        /// </summary>
        /// <param name="configure">The configuration callback.</param>
        public BeeTestFixtureBuilder ConfigureBackend(Action<BackendConfiguration> configure)
        {
            _configureBackend = configure ?? throw new ArgumentNullException(nameof(configure));
            return this;
        }

        internal PathOptions BuildPathOptions(out string? tempDir)
        {
            var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
            var sharedDefine = Path.Combine(repoRoot, "tests", "Define");

            if (!_useTempDefinePath)
            {
                tempDir = null;
                return new PathOptions { DefinePath = sharedDefine };
            }

            tempDir = Path.Combine(Path.GetTempPath(), $"bee-fixture-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            CopyDirectory(sharedDefine, tempDir);
            return new PathOptions { DefinePath = tempDir };
        }

        internal ServiceProvider BuildServiceProvider(PathOptions paths)
        {
            var settings = SystemSettingsLoader.Load(paths);
            settings.BackendConfiguration.Components.BusinessObjectFactory = BackendDefaultTypes.BusinessObjectFactory;

            // CI uses an environment variable instead of a file-backed master key so that
            // MasterKeyProviderTests assertions about "DefinePath has no Master.key" hold.
            if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
            {
                settings.BackendConfiguration.SecurityKeySettings.MasterKeySource = new MasterKeySource
                {
                    Type = MasterKeySourceType.Environment,
                    Value = "BEE_TEST_FIXTURE_MASTER_KEY"
                };
            }

            _configureBackend?.Invoke(settings.BackendConfiguration);

            var services = new ServiceCollection();
            services.AddBeeFramework(settings.BackendConfiguration, paths, autoCreateMasterKey: true);
            return services.BuildServiceProvider();
        }

        private static string FindRepoRoot(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                if (dir.GetDirectories(".git").Length > 0)
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException($"Cannot find repo root from: {startDir}");
        }

        private static void CopyDirectory(string source, string dest)
        {
            foreach (var subdir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(source, subdir);
                Directory.CreateDirectory(Path.Combine(dest, rel));
            }
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(source, file);
                File.Copy(file, Path.Combine(dest, rel), overwrite: true);
            }
        }
    }
}
