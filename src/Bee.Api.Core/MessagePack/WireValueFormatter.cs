using System.Data;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Reads one wire value off the reader.
    /// </summary>
    internal delegate object? WireValueReader(ref MessagePackReader reader, MessagePackSerializerOptions options);

    /// <summary>
    /// Writes one wire value to the writer.
    /// </summary>
    internal delegate void WireValueWriter(object value, ref MessagePackWriter writer, MessagePackSerializerOptions options);

    /// <summary>
    /// Discriminators for the value types the framework carries on its <see cref="object"/>-typed
    /// wire members. Values are part of the wire format and must not be renumbered.
    /// </summary>
    internal static class WireValueCode
    {
        public const int Boolean = 1;
        public const int Byte = 2;
        public const int SByte = 3;
        public const int Int16 = 4;
        public const int UInt16 = 5;
        public const int Int32 = 6;
        public const int UInt32 = 7;
        public const int Int64 = 8;
        public const int UInt64 = 9;
        public const int Single = 10;
        public const int Double = 11;
        public const int Decimal = 12;
        public const int String = 13;
        public const int DateTime = 14;
        public const int DateTimeOffset = 15;
        public const int TimeSpan = 16;
        public const int DateOnly = 17;
        public const int Guid = 18;
        public const int ByteArray = 19;
        public const int DBNull = 20;
        public const int DataTable = 21;
        public const int ObjectArray = 22;

        /// <summary>
        /// One past the highest code; the size of the dispatch tables.
        /// </summary>
        public const int Count = 23;
    }

    /// <summary>
    /// Serializes an <see cref="object"/>-typed wire member (a filter value, a parameter value, a
    /// table cell) as a two-element envelope: a discriminator followed by the value.
    /// </summary>
    /// <remarks>
    /// WARNING: This replaces <c>TypelessFormatter</c> on the framework's own value types for a
    /// reason. <c>TypelessFormatter</c> resolves the value's formatter through
    /// <c>MessagePackSerializer.NonGeneric</c>, which needs <c>Reflection.Emit</c> to pass the
    /// `ref struct` writer — so on iOS every non-primitive value (<c>Guid</c>, <c>DateTime</c>,
    /// <c>Decimal</c>, <c>DateOnly</c>) failed while <c>String</c> and <c>Int32</c> came through
    /// on its primitive fast path. Here each known type has a closed generic serializer built at
    /// class-initialisation time, so the whole path stays generic.
    /// <para>
    /// The discriminator is an <c>int</c> for the known set and a <c>string</c> type name for
    /// anything else. The string branch is the escape hatch for the application-configured
    /// namespaces (<c>SysInfo.AllowedTypeNamespaces</c>): it still goes through the non-generic
    /// overload and therefore still only works where dynamic code does. That is a deliberate
    /// trade — it keeps the existing extensibility on the server without holding the framework's
    /// own value types hostage to it.
    /// </para>
    /// <para>
    /// NOTE: This envelope replaced the ext-type-100 framing <c>TypelessFormatter</c> emits, which
    /// carried a full assembly-qualified type name for every value. The wire is therefore not
    /// compatible with releases before this change; it is also smaller and no longer names CLR
    /// assemblies on the wire for the known set.
    /// </para>
    /// </remarks>
    internal sealed class WireValueFormatter : IMessagePackFormatter<object?>
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly WireValueFormatter Instance = new WireValueFormatter();

        private static readonly Dictionary<Type, int> s_codes = [];
        private static readonly WireValueWriter?[] s_writers = new WireValueWriter?[WireValueCode.Count];
        private static readonly WireValueReader?[] s_readers = new WireValueReader?[WireValueCode.Count];

        private WireValueFormatter() { }

        static WireValueFormatter()
        {
            Add(WireValueCode.Boolean, static v => (bool)v);
            Add(WireValueCode.Byte, static v => (byte)v);
            Add(WireValueCode.SByte, static v => (sbyte)v);
            Add(WireValueCode.Int16, static v => (short)v);
            Add(WireValueCode.UInt16, static v => (ushort)v);
            Add(WireValueCode.Int32, static v => (int)v);
            Add(WireValueCode.UInt32, static v => (uint)v);
            Add(WireValueCode.Int64, static v => (long)v);
            Add(WireValueCode.UInt64, static v => (ulong)v);
            Add(WireValueCode.Single, static v => (float)v);
            Add(WireValueCode.Double, static v => (double)v);
            Add(WireValueCode.Decimal, static v => (decimal)v);
            Add(WireValueCode.String, static v => (string)v);
            Add(WireValueCode.DateTime, static v => (DateTime)v);
            Add(WireValueCode.DateTimeOffset, static v => (DateTimeOffset)v);
            Add(WireValueCode.TimeSpan, static v => (TimeSpan)v);
            Add(WireValueCode.DateOnly, static v => (DateOnly)v);
            Add(WireValueCode.Guid, static v => (Guid)v);
            Add(WireValueCode.ByteArray, static v => (byte[])v);
            Add(WireValueCode.DataTable, static v => (DataTable)v);

            // DBNull and object[] are not plain values: the first carries no payload, the second
            // recurses through this very formatter rather than through an array formatter, which
            // the resolver would otherwise have to build with MakeGenericType.
            AddCustom(
                WireValueCode.DBNull,
                typeof(DBNull),
                static (object _, ref MessagePackWriter writer, MessagePackSerializerOptions _) => writer.WriteNil(),
                static (ref MessagePackReader reader, MessagePackSerializerOptions _) =>
                {
                    reader.Skip();
                    return System.DBNull.Value;
                });

            AddCustom(
                WireValueCode.ObjectArray,
                typeof(object[]),
                static (object value, ref MessagePackWriter writer, MessagePackSerializerOptions options) =>
                {
                    var array = (object?[])value;
                    writer.WriteArrayHeader(array.Length);
                    foreach (var element in array)
                        Instance.Serialize(ref writer, element, options);
                },
                static (ref MessagePackReader reader, MessagePackSerializerOptions options) =>
                {
                    var count = reader.ReadArrayHeader();
                    var array = new object?[count];
                    for (var i = 0; i < count; i++)
                        array[i] = Instance.Deserialize(ref reader, options);
                    return array;
                });
        }

        /// <summary>
        /// Registers a value type whose payload is whatever its own formatter writes.
        /// <typeparamref name="TValue"/> is fixed at compile time, which is the point: the
        /// serializer call below is a closed generic and needs no dynamic code.
        /// </summary>
        private static void Add<TValue>(int code, Func<object, TValue> cast)
        {
            AddCustom(
                code,
                typeof(TValue),
                (object value, ref MessagePackWriter writer, MessagePackSerializerOptions options)
                    => MessagePackSerializer.Serialize(ref writer, cast(value), options),
                (ref MessagePackReader reader, MessagePackSerializerOptions options)
                    => MessagePackSerializer.Deserialize<TValue>(ref reader, options));
        }

        /// <summary>
        /// Registers a value type with hand-written framing.
        /// </summary>
        private static void AddCustom(int code, Type type, WireValueWriter writer, WireValueReader reader)
        {
            s_codes.Add(type, code);
            s_writers[code] = writer;
            s_readers[code] = reader;
        }

        /// <summary>
        /// Serializes the value.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, object? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(2);

            var type = value.GetType();
            if (s_codes.TryGetValue(type, out var code))
            {
                writer.Write(code);
                s_writers[code]!(value, ref writer, options);
                return;
            }

            // NOTE: Both ends of this escape hatch must screen the same shape. The writer holds a
            // `Type`, the reader holds a name, so they call different overloads — but both walk
            // generic arguments and array element types rather than testing one flat string.
            var fullName = type.FullName
                ?? throw new InvalidOperationException("Cannot serialize a type with no FullName.");
            if (!WireTypeWhitelist.IsRuntimeTypeAllowed(type))
            {
                throw new InvalidOperationException(
                    $"MessagePack serialization blocked: type '{fullName}' is not in the allowed type whitelist.");
            }

            writer.Write(type.AssemblyQualifiedName);
            MessagePackSerializer.Serialize(type, ref writer, value, options);
        }

        /// <summary>
        /// Deserializes the value.
        /// </summary>
        public object? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            options.Security.DepthStep(ref reader);
            try
            {
                var count = reader.ReadArrayHeader();
                if (count != 2)
                    throw new MessagePackSerializationException($"Unexpected wire value envelope length {count}.");

                if (reader.NextMessagePackType == MessagePackType.Integer)
                {
                    var code = reader.ReadInt32();
                    var read = code >= 0 && code < s_readers.Length ? s_readers[code] : null;
                    if (read == null)
                        throw new MessagePackSerializationException($"Unknown wire value code {code}.");
                    return read(ref reader, options);
                }

                var typeName = reader.ReadString()
                    ?? throw new MessagePackSerializationException("Wire value envelope has no type name.");

                // WARNING: The name is screened before `Type.GetType` resolves it, so a disallowed
                // type is never even loaded. Do not reorder these two — the whole point of the
                // whitelist is that it runs ahead of anything the payload can influence.
                // The screen covers generic arguments as well; screening only the text before the
                // first comma leaves them unchecked, because a generic argument's own comma comes
                // first (see `WireTypeWhitelist.IsAssemblyQualifiedNameAllowed`).
                if (!WireTypeWhitelist.IsAssemblyQualifiedNameAllowed(typeName))
                {
                    throw new InvalidOperationException(
                        $"MessagePack deserialization blocked: type '{typeName}' is not in the allowed type whitelist.");
                }

                var type = Type.GetType(typeName)
                    ?? throw new InvalidOperationException($"MessagePack deserialization blocked: unknown type '{typeName}'.");
                options.ThrowIfDeserializingTypeIsDisallowed(type);
                return MessagePackSerializer.Deserialize(type, ref reader, options);
            }
            finally
            {
                reader.Depth--;
            }
        }

    }
}
