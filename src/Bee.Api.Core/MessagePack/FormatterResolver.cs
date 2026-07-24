using Bee.Definition.Collections;
using System.Collections.Concurrent;
using System.Data;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Custom MessagePack formatter resolver that registers dedicated formatters for DataSet, DataTable, and TCollectionBase types.
    /// </summary>
    /// <remarks>
    /// WARNING: This resolver is currently unreachable in practice, so do not rely on it as a safety
    /// net. Two reasons:
    /// <list type="number">
    ///   <item><description>
    ///     In <c>MessagePackCodec.Options</c> it is registered *after*
    ///     <c>ContractlessStandardResolver</c>, which already claims virtually every class type.
    ///   </description></item>
    ///   <item><description>
    ///     Every <c>MessagePackCollectionBase&lt;&gt;</c> collection in use is explicitly registered as a
    ///     <c>CollectionBaseFormatter</c> in that same options object, and explicit formatters win over
    ///     resolvers regardless.
    ///   </description></item>
    /// </list>
    /// Moving it ahead of <c>ContractlessStandardResolver</c> is NOT a drop-in fix: its fallback below
    /// delegates to <c>StandardResolver</c>, which requires <c>[MessagePackObject]</c> attributes, so
    /// every contractless type (<c>FormSchema</c>, <c>FormLayout</c>, and friends) would fail. Making
    /// this a real safety net would require the fallback to return null and let the composite resolver
    /// continue, plus a recursive base-type check (the test below only matches a direct base type).
    /// Until then: a new <c>MessagePackCollectionBase&lt;&gt;</c> collection MUST be registered explicitly
    /// in <c>MessagePackCodec</c>, or it will serialize as an empty collection with no error.
    /// </remarks>
    internal class FormatterResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly FormatterResolver Instance = new FormatterResolver();

        private FormatterResolver() { }

        /// <summary>
        /// Cached formatter instances for improved performance.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> _formatters = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Gets the formatter for the specified type.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <returns>The formatter instance.</returns>
        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return (IMessagePackFormatter<T>?)_formatters.GetOrAdd(typeof(T), type =>
            {
                // Dedicated support for DataSet and DataTable
                if (type == typeof(DataSet))
                    return new DataSetFormatter();
                if (type == typeof(DataTable))
                    return new DataTableFormatter();

                // Check whether the type inherits from TCollectionBase<T>
                if (type.IsClass && !type.IsAbstract && type.BaseType != null &&
                    type.BaseType.IsGenericType &&
                    type.BaseType.GetGenericTypeDefinition() == typeof(MessagePackCollectionBase<>))
                {
                    var elementType = type.BaseType.GetGenericArguments()[0];

                    // Construct the generic formatter type
                    var formatterType = typeof(CollectionBaseFormatter<,>).MakeGenericType(type, elementType);

                    // Create and return the instance
                    return Activator.CreateInstance(formatterType)!;
                }

                // Fallback: delegate to the standard resolver
                var method = typeof(StandardResolver).GetMethod("GetFormatter")!
                    .MakeGenericMethod(type);
                return method.Invoke(StandardResolver.Instance, null)!;
            });
        }
    }

}
