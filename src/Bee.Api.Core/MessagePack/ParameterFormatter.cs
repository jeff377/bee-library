using Bee.Definition.Collections;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializes <see cref="Parameter"/> as a property-name keyed map, carrying only the wire members.
    /// </summary>
    /// <remarks>
    /// Hand-written rather than reflection-driven on purpose: a generic formatter would have to
    /// recurse through the non-generic <c>Serialize(Type, ref MessagePackWriter, ...)</c> overload,
    /// which needs <c>Reflection.Emit</c> to pass the `ref struct` writer and therefore throws on
    /// the mobile heads. Naming every member at compile time keeps the whole path generic.
    /// <para>
    /// WARNING: Adding a property to <see cref="Parameter"/> means adding it here too. The guard is the
    /// <see cref="WireMemberCount"/> assertion in the wire tests, which fails as soon as the two
    /// shapes drift apart.
    /// </para>
    /// </remarks>
    internal sealed class ParameterFormatter : IMessagePackFormatter<Parameter?>
    {
        /// <summary>
        /// Number of members written, asserted by the wire tests.
        /// </summary>
        public const int WireMemberCount = 2;

        /// <summary>
        /// Serializes the value.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, Parameter? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(WireMemberCount);

            writer.Write(nameof(Parameter.Name));
            MessagePackSerializer.Serialize<string>(ref writer, value.Name, options);

            writer.Write(nameof(Parameter.Value));
            MessagePackSerializer.Serialize<object?>(ref writer, value.Value, options);
        }

        /// <summary>
        /// Deserializes the value, skipping keys this version does not know.
        /// </summary>
        public Parameter? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            options.Security.DepthStep(ref reader);
            try
            {
                var result = new Parameter();
                var count = reader.ReadMapHeader();
                for (var i = 0; i < count; i++)
                {
                    switch (reader.ReadString())
                    {
                        case nameof(Parameter.Name):
                            result.Name = MessagePackSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case nameof(Parameter.Value):
                            result.Value = MessagePackSerializer.Deserialize<object?>(ref reader, options);
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
