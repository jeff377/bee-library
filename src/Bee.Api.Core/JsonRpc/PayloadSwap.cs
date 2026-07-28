namespace Bee.Api.Core.JsonRpc
{
    /// <summary>
    /// Undoes a request-payload swap, restoring the objects the caller passed in.
    /// </summary>
    /// <remarks>
    /// A default-constructed instance is the "nothing was swapped" case and disposes harmlessly, so
    /// call sites need no null check.
    /// </remarks>
    public readonly struct PayloadSwap : IDisposable, IEquatable<PayloadSwap>
    {
        private readonly Action? _restore;

        /// <summary>
        /// Initializes a swap that runs <paramref name="restore"/> on disposal.
        /// </summary>
        /// <param name="restore">The action that puts the caller's objects back.</param>
        public PayloadSwap(Action restore) { _restore = restore; }

        /// <summary>
        /// Restores the caller's objects.
        /// </summary>
        public void Dispose() => _restore?.Invoke();

        /// <inheritdoc/>
        public bool Equals(PayloadSwap other) => Equals(_restore, other._restore);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is PayloadSwap other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => _restore?.GetHashCode() ?? 0;

        /// <summary>Determines whether two swaps wrap the same restore action.</summary>
        public static bool operator ==(PayloadSwap left, PayloadSwap right) => left.Equals(right);

        /// <summary>Determines whether two swaps wrap different restore actions.</summary>
        public static bool operator !=(PayloadSwap left, PayloadSwap right) => !left.Equals(right);
    }
}
