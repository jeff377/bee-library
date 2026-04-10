using System;
using Bee.Definition.Serialization;
using MessagePack;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Custom <see cref="MessagePackSerializerOptions"/> that enforces the allowed type whitelist
    /// during <see cref="MessagePack.Formatters.TypelessFormatter"/> deserialization.
    /// </summary>
    /// <remarks>
    /// <see cref="MessagePack.Formatters.TypelessFormatter"/> calls
    /// <see cref="MessagePackSerializerOptions.ThrowIfDeserializingTypeIsDisallowed"/>
    /// <b>before</b> instantiating the deserialized object. This override applies
    /// <see cref="SafeTypelessFormatter.IsTypeAllowed"/> at that point, preventing
    /// untrusted types from being constructed.
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
        /// Called by <see cref="MessagePack.Formatters.TypelessFormatter"/> during deserialization.
        /// </summary>
        /// <param name="type">The type about to be instantiated.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="type"/> is not in the allowed whitelist.
        /// </exception>
        public override void ThrowIfDeserializingTypeIsDisallowed(Type type)
        {
            // Apply the built-in blocklist first (known-dangerous types)
            base.ThrowIfDeserializingTypeIsDisallowed(type);

            var fullName = type.FullName;
            if (fullName == null)
                throw new InvalidOperationException("Cannot deserialize a type with no FullName.");

            if (!SafeTypelessFormatter.IsTypeAllowed(fullName))
            {
                throw new InvalidOperationException(
                    $"MessagePack deserialization blocked: type '{fullName}' is not in the allowed type whitelist.");
            }
        }
    }
}
