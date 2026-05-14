using Bee.Definition.Collections;
using Bee.Base.Collections;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// MessagePack formatter for serializing and deserializing strongly-typed collections that inherit from <see cref="MessagePackCollectionBase{T}"/>.
    /// </summary>
    /// <typeparam name="TCollection">The concrete collection type, which must inherit from <see cref="MessagePackCollectionBase{TElement}"/> and have a parameterless constructor.</typeparam>
    /// <typeparam name="TElement">The type of items in the collection.</typeparam>
    internal class CollectionBaseFormatter<TCollection, TElement> : IMessagePackFormatter<TCollection>
        where TCollection : MessagePackCollectionBase<TElement>, new()
        where TElement : class, ICollectionItem
    {
        /// <summary>
        /// Serializes the collection object to MessagePack format.
        /// </summary>
        /// <param name="writer">The MessagePack writer.</param>
        /// <param name="value">The collection to serialize; <c>null</c> is written as nil.</param>
        /// <param name="options">The serialization options.</param>
        public void Serialize(ref MessagePackWriter writer, TCollection value, MessagePackSerializerOptions options)
        {
            // Nullable collection fields on `[MessagePackObject]` types (e.g.
            // `SortFieldCollection? SortFields`) reach this formatter as null when
            // unset; emit nil so the wire format remains valid.
            if (value == null)
            {
                writer.WriteNil();
                return;
            }

            writer.WriteArrayHeader(value.Count);

            foreach (var item in value)
            {
                MessagePackSerializer.Serialize(ref writer, item, options);
            }
        }

        /// <summary>
        /// Deserializes MessagePack format data into a collection object.
        /// </summary>
        /// <param name="reader">The MessagePack reader.</param>
        /// <param name="options">The deserialization options.</param>
        /// <returns>The deserialized collection object, or <c>null</c> when the wire format is nil.</returns>
        public TCollection Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null!;

            var count = reader.ReadArrayHeader();
            var collection = new TCollection();

            for (int i = 0; i < count; i++)
            {
                var element = MessagePackSerializer.Deserialize<TElement>(ref reader, options);
                collection.Add(element);
            }

            return collection;
        }
    }

}
