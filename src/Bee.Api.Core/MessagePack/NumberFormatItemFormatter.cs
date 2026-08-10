using Bee.Definition;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializes <see cref="NumberFormatItem"/> as a property-name keyed map, carrying only the wire members.
    /// </summary>
    /// <remarks>
    /// Hand-written rather than reflection-driven on purpose: a generic formatter would have to
    /// recurse through the non-generic <c>Serialize(Type, ref MessagePackWriter, ...)</c> overload,
    /// which needs <c>Reflection.Emit</c> to pass the `ref struct` writer and therefore throws on
    /// the mobile heads. Naming every member at compile time keeps the whole path generic.
    /// <para>
    /// WARNING: Adding a property to <see cref="NumberFormatItem"/> means adding it here too. The guard is the
    /// <see cref="WireMemberCount"/> assertion in the wire tests, which fails as soon as the two
    /// shapes drift apart.
    /// </para>
    /// </remarks>
    internal sealed class NumberFormatItemFormatter : IMessagePackFormatter<NumberFormatItem?>
    {
        /// <summary>
        /// Number of members written, asserted by the wire tests.
        /// </summary>
        public const int WireMemberCount = 2;

        /// <summary>
        /// Serializes the value.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, NumberFormatItem? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(WireMemberCount);

            writer.Write(nameof(NumberFormatItem.Kind));
            MessagePackSerializer.Serialize<NumberKind>(ref writer, value.Kind, options);

            writer.Write(nameof(NumberFormatItem.Decimals));
            MessagePackSerializer.Serialize<int>(ref writer, value.Decimals, options);
        }

        /// <summary>
        /// Deserializes the value, skipping keys this version does not know.
        /// </summary>
        public NumberFormatItem? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            options.Security.DepthStep(ref reader);
            try
            {
                var result = new NumberFormatItem();
                var count = reader.ReadMapHeader();
                for (var i = 0; i < count; i++)
                {
                    switch (reader.ReadString())
                    {
                        case nameof(NumberFormatItem.Kind):
                            result.Kind = MessagePackSerializer.Deserialize<NumberKind>(ref reader, options);
                            break;
                        case nameof(NumberFormatItem.Decimals):
                            result.Decimals = MessagePackSerializer.Deserialize<int>(ref reader, options);
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
