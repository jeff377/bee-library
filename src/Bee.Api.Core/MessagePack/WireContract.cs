using MessagePack;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Entry point for declaring a wire contract.
    /// </summary>
    internal static class WireContract
    {
        /// <summary>
        /// Starts a contract for <typeparamref name="T"/>, constructed with its parameterless constructor.
        /// </summary>
        public static WireContractBuilder<T> For<T>() where T : class, new()
            => new WireContractBuilder<T>(static () => new T());

        /// <summary>
        /// Starts a contract for <typeparamref name="T"/> with an explicit factory, for types with
        /// no parameterless constructor.
        /// </summary>
        public static WireContractBuilder<T> For<T>(Func<T> factory) where T : class
            => new WireContractBuilder<T>(factory);
    }

    /// <summary>
    /// Collects the members of a wire contract and builds the formatter.
    /// </summary>
    /// <typeparam name="T">The wire type.</typeparam>
    internal sealed class WireContractBuilder<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly List<string> _names = [];
        private readonly List<WireMemberWriter<T>> _writers = [];
        private readonly Dictionary<string, WireMemberReader<T>> _readers = new(StringComparer.Ordinal);

        internal WireContractBuilder(Func<T> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Declares a member. <typeparamref name="TValue"/> is fixed at compile time, which is what
        /// keeps the serializer call generic and therefore usable where dynamic code is not.
        /// </summary>
        /// <param name="name">The wire key; use <c>nameof</c> so a rename cannot drift.</param>
        /// <param name="get">Reads the member from the instance.</param>
        /// <param name="set">Writes the member back onto the instance.</param>
        public WireContractBuilder<T> Member<TValue>(string name, Func<T, TValue> get, Action<T, TValue> set)
        {
            _names.Add(name);
            _writers.Add((T value, ref MessagePackWriter writer, MessagePackSerializerOptions options)
                => MessagePackSerializer.Serialize(ref writer, get(value), options));
            _readers.Add(name, (T target, ref MessagePackReader reader, MessagePackSerializerOptions options)
                => set(target, MessagePackSerializer.Deserialize<TValue>(ref reader, options)));
            return this;
        }

        /// <summary>
        /// Builds the formatter.
        /// </summary>
        public WireObjectFormatter<T> Build()
            => new WireObjectFormatter<T>(_factory, _names.ToArray(), _writers.ToArray(), _readers);
    }
}
