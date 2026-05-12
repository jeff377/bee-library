using Microsoft.Extensions.DependencyInjection;

namespace Bee.Tests.Shared
{
    /// <summary>
    /// Process-wide DI container for tests. Built once by <see cref="GlobalFixture"/> and
    /// reused across all test classes. Phase 4 transitional helper — replaced by per-class
    /// ServiceProvider in Phase 5.
    /// </summary>
    public static class BeeTestServices
    {
        private static IServiceProvider? _provider;

        /// <summary>
        /// Installs the process-wide service provider. Called from <see cref="GlobalFixture"/>
        /// after <c>AddBeeFramework + BuildServiceProvider</c>.
        /// </summary>
        internal static void Initialize(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Gets the process-wide service provider. Throws when accessed before
        /// <see cref="GlobalFixture"/> has initialized.
        /// </summary>
        public static IServiceProvider Provider =>
            _provider ?? throw new InvalidOperationException(
                "BeeTestServices not initialized. Ensure GlobalFixture has run before accessing.");

        /// <summary>
        /// Resolves a required service from the process-wide provider.
        /// </summary>
        public static T GetRequiredService<T>() where T : notnull
            => Provider.GetRequiredService<T>();

        /// <summary>
        /// Resolves an optional service from the process-wide provider; returns null when absent.
        /// </summary>
        public static T? GetService<T>() where T : class
            => Provider.GetService<T>();
    }
}
