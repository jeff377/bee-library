namespace Bee.Definition
{
    /// <summary>
    /// Minimal generic extensions on <see cref="IServiceProvider"/>.
    /// Internal so it does not collide with
    /// <c>Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions</c>
    /// when host code consumes both namespaces; exposed via <c>InternalsVisibleTo</c>
    /// to <c>Bee.Business</c> so BO base classes can use it for the rare per-method
    /// service lookups.
    /// </summary>
    /// <remarks>
    /// WARNING: <c>internal</c> avoids the collision at the package boundary, not inside
    /// <c>Bee.Business</c> — and that is where the trap is. Any file there that both
    /// <c>using Microsoft.Extensions.DependencyInjection;</c> and calls
    /// <c>GetService&lt;T&gt;</c> / <c>GetRequiredService&lt;T&gt;</c> gets <b>CS0121</b>: two
    /// extension methods, equally applicable, neither preferred.
    /// <para>
    /// It compiles today by coincidence rather than by design. Exactly one file in
    /// <c>Bee.Business</c> carries that using (<c>FormPluginRunner.cs</c>) and it happens to make
    /// no such call. Adding the call there — or adding the using to any of the ~50 files that do
    /// call these — is enough. The fix when it happens is to qualify the call, not to delete
    /// either extension.
    /// </para>
    /// </remarks>
    internal static class BeeServiceProviderExtensions
    {
        /// <summary>
        /// Resolves a service of type <typeparamref name="T"/>; returns <c>null</c>
        /// when not registered.
        /// </summary>
        public static T? GetService<T>(this IServiceProvider sp) where T : class
            => sp.GetService(typeof(T)) as T;

        /// <summary>
        /// Resolves a service of type <typeparamref name="T"/>; throws when not registered.
        /// </summary>
        /// <exception cref="InvalidOperationException">When the service is not registered.</exception>
        public static T GetRequiredService<T>(this IServiceProvider sp) where T : class
            => sp.GetService<T>() ?? throw new InvalidOperationException(
                $"Required service of type {typeof(T)} not found.");
    }
}
