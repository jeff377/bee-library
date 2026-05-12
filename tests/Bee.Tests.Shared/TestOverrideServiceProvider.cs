namespace Bee.Tests.Shared
{
    /// <summary>
    /// Test helper that wraps an existing <see cref="IServiceProvider"/> and overrides a fixed
    /// set of service-type → instance mappings. Use this when a test needs to swap a single
    /// service (e.g. <c>ILoginAttemptTracker</c>) without rebuilding the full DI container.
    /// </summary>
    public sealed class TestOverrideServiceProvider : IServiceProvider
    {
        private readonly IServiceProvider _inner;
        private readonly Dictionary<Type, object?> _overrides;

        /// <summary>
        /// Initializes a new <see cref="TestOverrideServiceProvider"/>.
        /// </summary>
        /// <param name="inner">The wrapped provider; consulted for any service not in <paramref name="overrides"/>.</param>
        /// <param name="overrides">The service-type → instance overrides; <c>null</c> instances are
        /// honoured (and will short-circuit the inner provider's lookup).</param>
        public TestOverrideServiceProvider(IServiceProvider inner, params (Type ServiceType, object? Instance)[] overrides)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            ArgumentNullException.ThrowIfNull(overrides);
            _overrides = new Dictionary<Type, object?>(overrides.Length);
            foreach (var (type, instance) in overrides)
            {
                _overrides[type] = instance;
            }
        }

        /// <inheritdoc/>
        public object? GetService(Type serviceType)
        {
            if (_overrides.TryGetValue(serviceType, out var instance))
                return instance;
            return _inner.GetService(serviceType);
        }
    }
}
