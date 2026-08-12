using Bee.Definition.Filters;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Routes a statically typed <see cref="FilterGroup"/> through
    /// <see cref="FilterNodeFormatter"/>.
    /// </summary>
    /// <remarks>
    /// See <see cref="FilterConditionFormatter"/> for why the base-type registration is not enough.
    /// </remarks>
    internal sealed class FilterGroupFormatter : IMessagePackFormatter<FilterGroup?>, IWireContract
    {
        private static readonly FilterNodeFormatter s_inner = new FilterNodeFormatter();

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
            => s_inner.Serialize(ref writer, value, options);

        /// <summary>
        /// Deserializes the value.
        /// </summary>
        public FilterGroup? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            => (FilterGroup?)s_inner.Deserialize(ref reader, options);
    }
}
