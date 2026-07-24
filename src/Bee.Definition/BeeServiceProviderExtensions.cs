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
