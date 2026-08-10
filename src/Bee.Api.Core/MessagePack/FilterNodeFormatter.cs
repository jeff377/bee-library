using System.Buffers;
using Bee.Definition.Filters;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializes the polymorphic <see cref="FilterNode"/> hierarchy as a property-name keyed map
    /// carrying an explicit <c>Kind</c> discriminator.
    /// </summary>
    /// <remarks>
    /// Mirrors what <c>FilterNodeCollectionJsonConverter</c> already does on the JSON wire: read
    /// <see cref="FilterNode.Kind"/>, pick the concrete type, bind its members. Doing the same here
    /// gives the two formats one shared mental model, and lets the definition layer drop
    /// <c>[Union]</c> — the only reason it was pinned to integer keys.
    /// <para>
    /// The discriminator is written as a member rather than as a leading array element so the
    /// payload stays self-describing: a reader that logs the map sees <c>Kind</c> named, not a bare
    /// integer whose meaning lives in another file.
    /// </para>
    /// <para>
    /// WARNING: Adding a property to <see cref="FilterCondition"/> or <see cref="FilterGroup"/> —
    /// or adding a third subclass — means updating this formatter. The guard is the
    /// <c>WireMemberCount</c> assertions in the wire tests.
    /// </para>
    /// </remarks>
    internal sealed class FilterNodeFormatter : IMessagePackFormatter<FilterNode?>
    {
        /// <summary>
        /// Member name of the type discriminator.
        /// </summary>
        private const string KindKey = nameof(FilterNode.Kind);

        /// <summary>
        /// Members written for a <see cref="FilterCondition"/>, discriminator included.
        /// </summary>
        public const int ConditionWireMemberCount = 6;

        /// <summary>
        /// Members written for a <see cref="FilterGroup"/>, discriminator included.
        /// </summary>
        public const int GroupWireMemberCount = 3;

        /// <summary>
        /// Serializes the node, dispatching on its concrete type.
        /// </summary>
        /// <exception cref="MessagePackSerializationException">
        /// Thrown for a subclass this formatter does not know.
        /// </exception>
        public void Serialize(ref MessagePackWriter writer, FilterNode? value, MessagePackSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    writer.WriteNil();
                    return;

                case FilterCondition condition:
                    WriteCondition(ref writer, condition, options);
                    return;

                case FilterGroup group:
                    WriteGroup(ref writer, group, options);
                    return;

                default:
                    throw new MessagePackSerializationException(
                        $"No wire mapping for filter node type '{value.GetType().FullName}'. "
                        + "A new FilterNode subclass must be added to FilterNodeFormatter.");
            }
        }

        /// <summary>
        /// Deserializes the node, choosing the concrete type from the <c>Kind</c> discriminator.
        /// </summary>
        /// <remarks>
        /// The discriminator is not guaranteed to arrive first, so the payload is buffered as a
        /// sequence of key/value pairs and bound once the kind is known. Filter trees are small —
        /// a request-scoped predicate, not a data set — so the extra pass costs nothing measurable.
        /// </remarks>
        /// <exception cref="MessagePackSerializationException">
        /// Thrown when the discriminator is missing or unknown.
        /// </exception>
        public FilterNode? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            options.Security.DepthStep(ref reader);
            try
            {
                var count = reader.ReadMapHeader();
                FilterNodeKind? kind = null;
                var buffered = new List<(string Key, ReadOnlySequence<byte> Value)>(count);

                for (var i = 0; i < count; i++)
                {
                    var key = reader.ReadString() ?? string.Empty;
                    if (key == KindKey)
                    {
                        kind = (FilterNodeKind)reader.ReadInt32();
                        continue;
                    }

                    var start = reader.Position;
                    reader.Skip();
                    buffered.Add((key, reader.Sequence.Slice(start, reader.Position)));
                }

                return kind switch
                {
                    FilterNodeKind.Condition => ReadCondition(buffered, options),
                    FilterNodeKind.Group => ReadGroup(buffered, options),
                    _ => throw new MessagePackSerializationException(
                        $"Filter node payload carries no usable '{KindKey}' discriminator."),
                };
            }
            finally
            {
                reader.Depth--;
            }
        }

        /// <summary>
        /// Writes a <see cref="FilterCondition"/>.
        /// </summary>
        private static void WriteCondition(
            ref MessagePackWriter writer, FilterCondition value, MessagePackSerializerOptions options)
        {
            writer.WriteMapHeader(ConditionWireMemberCount);

            writer.Write(KindKey);
            writer.Write((int)FilterNodeKind.Condition);

            writer.Write(nameof(FilterCondition.FieldName));
            MessagePackSerializer.Serialize(ref writer, value.FieldName, options);

            writer.Write(nameof(FilterCondition.Operator));
            MessagePackSerializer.Serialize(ref writer, value.Operator, options);

            writer.Write(nameof(FilterCondition.Value));
            MessagePackSerializer.Serialize<object?>(ref writer, value.Value, options);

            writer.Write(nameof(FilterCondition.SecondValue));
            MessagePackSerializer.Serialize<object?>(ref writer, value.SecondValue, options);

            writer.Write(nameof(FilterCondition.IgnoreIfNull));
            MessagePackSerializer.Serialize(ref writer, value.IgnoreIfNull, options);
        }

        /// <summary>
        /// Writes a <see cref="FilterGroup"/>.
        /// </summary>
        private static void WriteGroup(
            ref MessagePackWriter writer, FilterGroup value, MessagePackSerializerOptions options)
        {
            writer.WriteMapHeader(GroupWireMemberCount);

            writer.Write(KindKey);
            writer.Write((int)FilterNodeKind.Group);

            writer.Write(nameof(FilterGroup.Operator));
            MessagePackSerializer.Serialize(ref writer, value.Operator, options);

            writer.Write(nameof(FilterGroup.Nodes));
            MessagePackSerializer.Serialize(ref writer, value.Nodes, options);
        }

        /// <summary>
        /// Binds the buffered members onto a <see cref="FilterCondition"/>.
        /// </summary>
        private static FilterCondition ReadCondition(
            List<(string Key, ReadOnlySequence<byte> Value)> buffered, MessagePackSerializerOptions options)
        {
            var result = new FilterCondition();
            foreach (var (key, value) in buffered)
            {
                switch (key)
                {
                    case nameof(FilterCondition.FieldName):
                        result.FieldName = Read<string>(value, options) ?? string.Empty;
                        break;
                    case nameof(FilterCondition.Operator):
                        result.Operator = Read<ComparisonOperator>(value, options);
                        break;
                    case nameof(FilterCondition.Value):
                        result.Value = Read<object?>(value, options);
                        break;
                    case nameof(FilterCondition.SecondValue):
                        result.SecondValue = Read<object?>(value, options);
                        break;
                    case nameof(FilterCondition.IgnoreIfNull):
                        result.IgnoreIfNull = Read<bool>(value, options);
                        break;
                    default:
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Binds the buffered members onto a <see cref="FilterGroup"/>.
        /// </summary>
        private static FilterGroup ReadGroup(
            List<(string Key, ReadOnlySequence<byte> Value)> buffered, MessagePackSerializerOptions options)
        {
            var result = new FilterGroup();
            foreach (var (key, value) in buffered)
            {
                switch (key)
                {
                    case nameof(FilterGroup.Operator):
                        result.Operator = Read<LogicalOperator>(value, options);
                        break;
                    case nameof(FilterGroup.Nodes):
                        result.Nodes = Read<FilterNodeCollection>(value, options) ?? [];
                        break;
                    default:
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Deserializes one buffered member value.
        /// </summary>
        private static T? Read<T>(ReadOnlySequence<byte> value, MessagePackSerializerOptions options)
        {
            var reader = new MessagePackReader(value);
            return MessagePackSerializer.Deserialize<T>(ref reader, options);
        }
    }
}
