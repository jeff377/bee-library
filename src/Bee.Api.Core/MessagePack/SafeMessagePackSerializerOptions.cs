using MessagePack;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Custom <see cref="MessagePackSerializerOptions"/> that enforces the allowed type whitelist
    /// when a payload names its own type.
    /// </summary>
    /// <remarks>
    /// The caller is <see cref="WireValueFormatter"/>'s named-type escape hatch — the only path left
    /// that resolves a type from the wire. It used to be MessagePack's <c>TypelessFormatter</c>,
    /// which ADR-037 removed.
    /// <para>
    /// That caller invokes
    /// <see cref="MessagePackSerializerOptions.ThrowIfDeserializingTypeIsDisallowed"/> <b>before</b>
    /// instantiating the deserialized object, and this override screens the type there — so a
    /// disallowed type is refused rather than constructed.
    /// </para>
    /// </remarks>
    internal sealed class SafeMessagePackSerializerOptions : MessagePackSerializerOptions
    {
        /// <summary>
        /// Initializes a new instance with the specified resolver.
        /// </summary>
        /// <param name="resolver">The formatter resolver to use.</param>
        public SafeMessagePackSerializerOptions(IFormatterResolver resolver)
            : base(resolver)
        {
        }

        /// <summary>
        /// Copy constructor used by <see cref="Clone"/>.
        /// </summary>
        private SafeMessagePackSerializerOptions(SafeMessagePackSerializerOptions copyFrom)
            : base(copyFrom)
        {
        }

        /// <inheritdoc />
        protected override MessagePackSerializerOptions Clone()
            => new SafeMessagePackSerializerOptions(this);

        /// <summary>
        /// Validates that the type is allowed for deserialization before object instantiation.
        /// Called by <see cref="WireValueFormatter"/> when a payload names its own type.
        /// </summary>
        /// <param name="type">The type about to be instantiated.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="type"/> is not in the allowed whitelist.
        /// </exception>
        public override void ThrowIfDeserializingTypeIsDisallowed(Type type)
        {
            var fullName = type.FullName;
            if (fullName == null)
                throw new InvalidOperationException("Cannot deserialize a type with no FullName.");

            // The fixed framework whitelist wins over the built-in blocklist:
            // MessagePack 3.1.5+ rejects `System.Data.DataTable` as a
            // BinaryFormatter gadget, but this wire rebuilds tables through the
            // framework's own formatter, so the type stays deliberately trusted.
            // See `WireTypeWhitelist.IsExplicitlyTrustedType` remarks.
            if (WireTypeWhitelist.IsExplicitlyTrustedType(fullName))
                return;

            // Apply the built-in blocklist (known-dangerous types) before the
            // application-level namespace whitelist.
            base.ThrowIfDeserializingTypeIsDisallowed(type);

            // WARNING: Screen the type's shape, not its `FullName`. A constructed generic embeds
            // its arguments in that one string, so testing it against the namespace whitelist
            // checks the outer type and lets every argument through.
            if (!WireTypeWhitelist.IsRuntimeTypeAllowed(type))
            {
                throw new InvalidOperationException(
                    $"MessagePack deserialization blocked: type '{fullName}' is not in the allowed type whitelist.");
            }
        }
    }
}
