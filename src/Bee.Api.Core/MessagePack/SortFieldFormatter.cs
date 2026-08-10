using Bee.Definition.Sorting;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializes <see cref="SortField"/> as a property-name keyed map, carrying only the wire
    /// members and leaving the framework-managed ones behind.
    /// </summary>
    /// <remarks>
    /// Hand-written rather than reflection-driven on purpose. A generic formatter would have to
    /// recurse through <c>MessagePackSerializer.Serialize(Type, ref MessagePackWriter, ...)</c>,
    /// and that non-generic overload needs <c>Reflection.Emit</c> to pass the `ref struct` writer —
    /// it throws <c>NotSupportedException</c> on the mobile heads, where dynamic code is
    /// unavailable. Naming every member at compile time keeps the whole path generic, so the same
    /// code runs on desktop and on device.
    /// <para>
    /// WARNING: Adding a property to <see cref="SortField"/> means adding it here too. Nothing in
    /// the compiler ties the two together — the guard is the member-count assertion in the wire
    /// round-trip tests, which fails as soon as the shapes drift apart.
    /// </para>
    /// </remarks>
    internal sealed class SortFieldFormatter : IMessagePackFormatter<SortField?>
    {
        /// <summary>
        /// Number of members written. Asserted by the wire tests so a new property cannot be added
        /// to <see cref="SortField"/> without this formatter being updated as well.
        /// </summary>
        public const int WireMemberCount = 2;

        /// <summary>
        /// Serializes the value.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, SortField? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(WireMemberCount);

            writer.Write(nameof(SortField.FieldName));
            MessagePackSerializer.Serialize(ref writer, value.FieldName, options);

            writer.Write(nameof(SortField.Direction));
            MessagePackSerializer.Serialize(ref writer, value.Direction, options);
        }

        /// <summary>
        /// Deserializes the value, skipping keys this version does not know.
        /// </summary>
        public SortField? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            options.Security.DepthStep(ref reader);
            try
            {
                var result = new SortField();
                var count = reader.ReadMapHeader();
                for (var i = 0; i < count; i++)
                {
                    switch (reader.ReadString())
                    {
                        case nameof(SortField.FieldName):
                            result.FieldName = MessagePackSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case nameof(SortField.Direction):
                            result.Direction = MessagePackSerializer.Deserialize<SortDirection>(ref reader, options);
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                return result;
            }
            finally
            {
                reader.Depth--;
            }
        }
    }
}
