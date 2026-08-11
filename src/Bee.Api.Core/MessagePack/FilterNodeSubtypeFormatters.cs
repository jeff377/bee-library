using Bee.Definition.Filters;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Routes a statically typed <see cref="FilterCondition"/> through
    /// <see cref="FilterNodeFormatter"/>.
    /// </summary>
    /// <remarks>
    /// A formatter registered for the base type does not cover the subclasses: a call site holding
    /// a <c>FilterCondition</c> resolves <c>IMessagePackFormatter&lt;FilterCondition&gt;</c>, which
    /// the contractless resolver used to build on demand — and cannot where dynamic code is off.
    /// The bytes are identical either way; the polymorphic formatter writes the same discriminated
    /// map whichever static type it was reached through.
    /// </remarks>
    internal sealed class FilterConditionFormatter : IMessagePackFormatter<FilterCondition?>, IWireContract
    {
        private static readonly FilterNodeFormatter Inner = new FilterNodeFormatter();

        /// <inheritdoc />
        public Type WireType => typeof(FilterCondition);

        /// <summary>
        /// Reports the member list the shared <see cref="FilterNodeFormatter"/> writes for this
        /// subtype, so the drift check has something to compare the type against. The base-type
        /// formatter cannot carry it: <see cref="FilterNode"/> is abstract and has no wire members
        /// of its own.
        /// </summary>
        public IReadOnlyList<string> WireMemberNames => FilterNodeFormatter.ConditionWireMembers;

        /// <summary>
        /// Serializes the value.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, FilterCondition? value, MessagePackSerializerOptions options)
            => Inner.Serialize(ref writer, value, options);

        /// <summary>
        /// Deserializes the value.
        /// </summary>
        public FilterCondition? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            => (FilterCondition?)Inner.Deserialize(ref reader, options);
    }

    /// <summary>
    /// Routes a statically typed <see cref="FilterGroup"/> through
    /// <see cref="FilterNodeFormatter"/>.
    /// </summary>
    /// <remarks>
    /// See <see cref="FilterConditionFormatter"/> for why the base-type registration is not enough.
    /// </remarks>
    internal sealed class FilterGroupFormatter : IMessagePackFormatter<FilterGroup?>, IWireContract
    {
        private static readonly FilterNodeFormatter Inner = new FilterNodeFormatter();

        /// <inheritdoc />
        public Type WireType => typeof(FilterGroup);

        /// <summary>
        /// Reports the member list the shared <see cref="FilterNodeFormatter"/> writes for this
        /// subtype, so the drift check has something to compare the type against. The base-type
        /// formatter cannot carry it: <see cref="FilterNode"/> is abstract and has no wire members
        /// of its own.
        /// </summary>
        public IReadOnlyList<string> WireMemberNames => FilterNodeFormatter.GroupWireMembers;

        /// <summary>
        /// Serializes the value.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, FilterGroup? value, MessagePackSerializerOptions options)
            => Inner.Serialize(ref writer, value, options);

        /// <summary>
        /// Deserializes the value.
        /// </summary>
        public FilterGroup? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            => (FilterGroup?)Inner.Deserialize(ref reader, options);
    }
}
