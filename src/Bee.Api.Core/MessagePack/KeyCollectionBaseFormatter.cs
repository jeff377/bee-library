using Bee.Base.Collections;
using Bee.Definition.Collections;
using MessagePack;
using MessagePack.Formatters;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializes a keyed collection as a plain array of its items, rebuilding the key index on
    /// the way back in.
    /// </summary>
    /// <remarks>
    /// The counterpart to <see cref="CollectionBaseFormatter{TCollection, TElement}"/> for the
    /// keyed variant. It is not optional the way that one is: the contractless resolver handles a
    /// <c>Collection&lt;T&gt;</c> natively, but binds a <c>KeyedCollection&lt;TKey, TItem&gt;</c>
    /// as a dictionary and hands back <c>Dictionary&lt;object, object&gt;</c> elements instead of
    /// the item type.
    /// <para>
    /// The key is not written — each item already carries it (<c>Key</c>, surfaced by subclasses
    /// under a domain name such as <c>Parameter.Name</c>), so <c>Add</c> re-derives the index and
    /// a serialized key would only be a second copy that could disagree with the first.
    /// </para>
    /// <para>
    /// This replaces the <c>ItemsForSerialization</c> proxy property that used to live on the
    /// collection base. That proxy existed only because a MessagePack <c>[Key]</c> attribute has
    /// to sit on a property; with the binding written out here, the definition layer needs neither
    /// the property nor the attribute.
    /// </para>
    /// </remarks>
    /// <typeparam name="TCollection">The keyed collection type.</typeparam>
    /// <typeparam name="TElement">The item type.</typeparam>
    internal sealed class KeyCollectionBaseFormatter<TCollection, TElement> : IMessagePackFormatter<TCollection?>
        where TCollection : MessagePackKeyCollectionBase<TElement>, new()
        where TElement : class, IKeyCollectionItem
    {
        /// <summary>
        /// Serializes the collection as an array of items.
        /// </summary>
        public void Serialize(ref MessagePackWriter writer, TCollection? value, MessagePackSerializerOptions options)
        {
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
        /// Deserializes the array, re-adding each item so the key index is rebuilt.
        /// </summary>
        public TCollection? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            options.Security.DepthStep(ref reader);
            try
            {
                var count = reader.ReadArrayHeader();
                var collection = new TCollection();
                for (var i = 0; i < count; i++)
                {
                    var element = MessagePackSerializer.Deserialize<TElement>(ref reader, options);
                    if (element != null)
                        collection.Add(element);
                }

                return collection;
            }
            finally
            {
                reader.Depth--;
            }
        }
    }
}
