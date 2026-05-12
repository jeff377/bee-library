using Bee.Definition.Identity;
using Bee.Definition.Storage;

namespace Bee.Definition
{
    /// <summary>
    /// Per-call context handed to business objects at construction time.
    /// Aggregates the cross-cutting services that virtually every BO method
    /// touches, plus an <see cref="IServiceProvider"/> escape hatch for rare
    /// per-method needs (e.g. login-only helpers).
    /// </summary>
    public interface IBeeContext
    {
        /// <summary>The definition data access service.</summary>
        IDefineAccess DefineAccess { get; }

        /// <summary>The session-info access service.</summary>
        ISessionInfoService SessionInfoService { get; }

        /// <summary>Factory for building business objects (used for BO-to-BO calls).</summary>
        IBusinessObjectFactory BoFactory { get; }

        /// <summary>
        /// Escape hatch for resolving services not in the typed core members.
        /// Use sparingly — reserved for rare per-method needs (e.g. login-only
        /// helpers used by <c>SystemBusinessObject.Login</c>). Greppable via
        /// <c>Services.GetService&lt;T&gt;</c> for audit.
        /// </summary>
        IServiceProvider Services { get; }
    }

    /// <summary>
    /// Default <see cref="IBeeContext"/> implementation; a plain POCO assembled
    /// by <c>BusinessObjectFactory</c> at BO construction time.
    /// </summary>
    public sealed class BeeContext : IBeeContext
    {
        /// <inheritdoc/>
        public required IDefineAccess DefineAccess { get; init; }

        /// <inheritdoc/>
        public required ISessionInfoService SessionInfoService { get; init; }

        /// <inheritdoc/>
        public required IBusinessObjectFactory BoFactory { get; init; }

        /// <inheritdoc/>
        public required IServiceProvider Services { get; init; }
    }

    /// <summary>
    /// Minimal generic extensions on <see cref="IServiceProvider"/>.
    /// Defined here to avoid taking a hard dependency on
    /// <c>Microsoft.Extensions.DependencyInjection.Abstractions</c>; Phase 4
    /// may switch to that package as part of full DI integration.
    /// </summary>
    public static class BeeServiceProviderExtensions
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
