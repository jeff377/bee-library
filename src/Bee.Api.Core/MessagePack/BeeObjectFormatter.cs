using System.Collections.Concurrent;
using System.Reflection;
using Bee.Base.Attributes;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializes an object as a property-name keyed map, honouring <see cref="WireIgnoreAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Behaviourally equivalent to the contractless resolver, with one addition: members marked
    /// <see cref="WireIgnoreAttribute"/> are excluded. That is the whole reason this type exists —
    /// it lets the definition layer declare "not on the wire" without referencing MessagePack.
    /// <para>
    /// Registered explicitly per type in <c>MessagePackCodec</c> rather than installed as a
    /// resolver, so it affects only the types that need the exclusion and leaves every other
    /// type on its existing resolution path.
    /// </para>
    /// <para>
    /// WARNING: Reflection only — no <c>Reflection.Emit</c>. The mobile heads run AOT where
    /// dynamic code generation is unavailable, so anything built on `Emit` would work on the
    /// desktop and fail on device.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The object type.</typeparam>
    internal sealed class BeeObjectFormatter<T> : IMessagePackFormatter<T?>
        where T : class, new()
    {
        /// <summary>
        /// Wire members, ordered by name so the layout is stable across runs.
        /// </summary>
        private static readonly PropertyInfo[] Members = BuildMembers();

        /// <summary>
        /// Name lookup for deserialization. Ordinal because these are identifiers, not display text.
        /// </summary>
        private static readonly ConcurrentDictionary<string, PropertyInfo> Lookup =
            new(Members.ToDictionary(p => p.Name, p => p, StringComparer.Ordinal),
                StringComparer.Ordinal);

        /// <summary>
        /// Collects the public read/write instance properties that are not wire-ignored.
        /// </summary>
        /// <remarks>
        /// A non-public setter is treated as read-only: state like <c>SerializeState</c> is
        /// managed by the framework, not restored from the payload.
        /// </remarks>
        private static PropertyInfo[] BuildMembers()
        {
            return typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0)
                .Where(p => p.GetMethod?.IsPublic == true && p.SetMethod?.IsPublic == true)
                .Where(p => p.GetCustomAttribute<WireIgnoreAttribute>() == null)
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Serializes the value as a property-name keyed map.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, T? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteMapHeader(Members.Length);
            foreach (var member in Members)
            {
                writer.Write(member.Name);
                MessagePackSerializer.Serialize(
                    member.PropertyType, ref writer, member.GetValue(value), options);
            }
        }

        /// <summary>
        /// Deserializes a property-name keyed map, skipping members this type no longer has.
        /// </summary>
        public T? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            options.Security.DepthStep(ref reader);
            try
            {
                var result = new T();
                var count = reader.ReadMapHeader();
                for (var i = 0; i < count; i++)
                {
                    var name = reader.ReadString();
                    if (name != null && Lookup.TryGetValue(name, out var member))
                    {
                        var value = MessagePackSerializer.Deserialize(
                            member.PropertyType, ref reader, options);
                        member.SetValue(result, value);
                    }
                    else
                    {
                        // An unknown key is a payload written by a version that had a member this
                        // one does not. Skipping keeps the reader aligned for the remaining pairs.
                        reader.Skip();
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
