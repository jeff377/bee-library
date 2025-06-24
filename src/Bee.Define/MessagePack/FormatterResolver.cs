using System;
using System.Collections.Concurrent;
using System.Data;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Bee.Define
{
    /// <summary>
    /// 自訂的 MessagePack 格式化器解析器，註冊 DataSet、DataTable 和 TCollectionBase 專用格式化器。
    /// </summary>
    public class FormatterResolver : IFormatterResolver
    {
        /// <summary>
        /// Singleton 實例。
        /// </summary>
        public static readonly FormatterResolver Instance = new FormatterResolver();

        private FormatterResolver() { }

        /// <summary>
        /// 快取格式化器實例以提升效能。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> _formatters = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// 取得指定型別的格式化器。
        /// </summary>
        /// <typeparam name="T">目標型別。</typeparam>
        /// <returns>格式化器實例。</returns>
        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return (IMessagePackFormatter<T>)_formatters.GetOrAdd(typeof(T), type =>
            {
                // 專屬支援 DataSet 及 DataTable
                if (type == typeof(DataSet))
                    return new DataSetFormatter();
                if (type == typeof(DataTable))
                    return new DataTableFormatter();

                // 檢查是否繼承自 TCollectionBase<T>
                if (type.IsClass && !type.IsAbstract && type.BaseType != null &&
                    type.BaseType.IsGenericType &&
                    type.BaseType.GetGenericTypeDefinition() == typeof(CollectionBase<>))
                {
                    var elementType = type.BaseType.GetGenericArguments()[0];

                    // 建立泛型 Formatter 類型
                    var formatterType = typeof(CollectionBaseFormatter<,>).MakeGenericType(type, elementType);

                    // 建立實例並回傳
                    return Activator.CreateInstance(formatterType);
                }

                // fallback：委託給標準解析器
                var method = typeof(StandardResolver).GetMethod("GetFormatter")
                    .MakeGenericMethod(type);
                return method.Invoke(StandardResolver.Instance, null);
            });
        }
    }

}
