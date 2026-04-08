using Bee.Definition.Collections;
using System;
using System.Collections.Concurrent;
using System.Data;
using Bee.Definition;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Custom MessagePack formatter resolver that registers dedicated formatters for DataSet, DataTable, and TCollectionBase types.
    /// </summary>
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
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return (IMessagePackFormatter<T>)_formatters.GetOrAdd(typeof(T), type =>
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
                    return Activator.CreateInstance(formatterType);
                }

                // Fallback: delegate to the standard resolver
                var method = typeof(StandardResolver).GetMethod("GetFormatter")
                    .MakeGenericMethod(type);
                return method.Invoke(StandardResolver.Instance, null);
            });
        }
    }

}
