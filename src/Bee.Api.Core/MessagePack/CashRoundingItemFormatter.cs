using Bee.Definition;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializes <see cref="CashRoundingItem"/> as a property-name keyed map, carrying only the wire members.
    /// </summary>
    /// <remarks>
    /// Hand-written rather than reflection-driven on purpose: a generic formatter would have to
    /// recurse through the non-generic <c>Serialize(Type, ref MessagePackWriter, ...)</c> overload,
    /// which needs <c>Reflection.Emit</c> to pass the `ref struct` writer and therefore throws on
    /// the mobile heads. Naming every member at compile time keeps the whole path generic.
    /// <para>
    /// WARNING: Adding a property to <see cref="CashRoundingItem"/> means adding it here too. The guard is
    /// <c>WireContractDriftTests</c>, which compares <see cref="WireMemberNames"/> against the
    /// type's actual shape and fails as soon as the two drift apart.
    /// </para>
    /// </remarks>
    internal sealed class CashRoundingItemFormatter : IMessagePackFormatter<CashRoundingItem?>, IWireContract
    {
        /// <summary>
        /// Wire member names, in write order. The single source for both the map header and the
        /// drift check — they cannot disagree because they read the same array.
        /// </summary>
        private static readonly string[] s_wireMembers =
        [
            nameof(CashRoundingItem.CurrencyCode),
            nameof(CashRoundingItem.Unit),
        ];

        /// <inheritdoc />
        public Type WireType => typeof(CashRoundingItem);

        /// <inheritdoc />
        public IReadOnlyList<string> WireMemberNames => s_wireMembers;

        /// <summary>
        /// Serializes the value.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, CashRoundingItem? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(s_wireMembers.Length);

            writer.Write(nameof(CashRoundingItem.CurrencyCode));
            MessagePackSerializer.Serialize<string>(ref writer, value.CurrencyCode, options);

            writer.Write(nameof(CashRoundingItem.Unit));
            MessagePackSerializer.Serialize<decimal>(ref writer, value.Unit, options);
        }

        /// <summary>
        /// Deserializes the value, skipping keys this version does not know.
        /// </summary>
        public CashRoundingItem? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            options.Security.DepthStep(ref reader);
            try
            {
                var result = new CashRoundingItem();
                var count = reader.ReadMapHeader();
                for (var i = 0; i < count; i++)
                {
                    switch (reader.ReadString())
                    {
                        case nameof(CashRoundingItem.CurrencyCode):
                            result.CurrencyCode = MessagePackSerializer.Deserialize<string>(ref reader, options);
                            break;
                        case nameof(CashRoundingItem.Unit):
                            result.Unit = MessagePackSerializer.Deserialize<decimal>(ref reader, options);
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
