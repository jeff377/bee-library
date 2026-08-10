using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Writes a member of <typeparamref name="T"/> to the wire.
    /// </summary>
    /// <remarks>
    /// A delegate rather than a lambda captured inside the formatter so the value type stays a
    /// compile-time generic argument all the way down to
    /// <see cref="MessagePackSerializer.Serialize{TValue}(ref MessagePackWriter, TValue, MessagePackSerializerOptions)"/>.
    /// That is the whole point of this machinery — see <see cref="WireObjectFormatter{T}"/>.
    /// </remarks>
    internal delegate void WireMemberWriter<in T>(T value, ref MessagePackWriter writer, MessagePackSerializerOptions options);

    /// <summary>
    /// Reads a member of <typeparamref name="T"/> from the wire and assigns it.
    /// </summary>
    internal delegate void WireMemberReader<in T>(T target, ref MessagePackReader reader, MessagePackSerializerOptions options);

    /// <summary>
    /// Serializes a wire type as a property-name keyed map, driven by a member table built at
    /// registration time.
    /// </summary>
    /// <remarks>
    /// WARNING: The reason this exists rather than a reflection-driven formatter is the mobile
    /// heads. Recursing over an arbitrary property type by reflection can only go through the
    /// non-generic <c>MessagePackSerializer.Serialize(Type, ref MessagePackWriter, ...)</c>
    /// overload, and <c>MessagePackWriter</c> is a `ref struct`: that overload needs
    /// <c>Reflection.Emit</c> to build a delegate able to pass it, which iOS does not have.
    /// <para>
    /// Here every member is registered through
    /// <see cref="WireContractBuilder{T}.Member{TValue}"/>, whose <c>TValue</c> is known at compile
    /// time, so each member's serializer is an ordinary closed generic call. No reflection, no
    /// dynamic code, and the desktop and the device walk the same path.
    /// </para>
    /// <para>
    /// The contract is the same one JSON uses: every public read/write property that is not
    /// <c>[JsonIgnore]</c>. <c>WireContractDriftTests</c> asserts that for every registered type,
    /// so a property added to a wire type without being registered here fails the build.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The wire type.</typeparam>
    internal sealed class WireObjectFormatter<T> : IMessagePackFormatter<T?>, IWireContract
        where T : class
    {
        private readonly Func<T> _factory;
        private readonly string[] _names;
        private readonly WireMemberWriter<T>[] _writers;
        private readonly Dictionary<string, WireMemberReader<T>> _readers;

        internal WireObjectFormatter(
            Func<T> factory,
            string[] names,
            WireMemberWriter<T>[] writers,
            Dictionary<string, WireMemberReader<T>> readers)
        {
            _factory = factory;
            _names = names;
            _writers = writers;
            _readers = readers;
        }

        /// <summary>
        /// Gets the type this contract covers.
        /// </summary>
        public Type WireType => typeof(T);

        /// <summary>
        /// Gets the member names written to the wire, in write order.
        /// </summary>
        public IReadOnlyList<string> WireMemberNames => _names;

        /// <summary>
        /// Serializes the value as a map keyed by property name.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, T? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(_names.Length);
            for (var i = 0; i < _names.Length; i++)
            {
                writer.Write(_names[i]);
                _writers[i](value, ref writer, options);
            }
        }

        /// <summary>
        /// Deserializes the value, skipping keys this version does not know.
        /// </summary>
        public T? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            options.Security.DepthStep(ref reader);
            try
            {
                var result = _factory();
                var count = reader.ReadMapHeader();
                for (var i = 0; i < count; i++)
                {
                    var name = reader.ReadString();
                    if (name != null && _readers.TryGetValue(name, out var read))
                        read(result, ref reader, options);
                    else
                        reader.Skip();
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
