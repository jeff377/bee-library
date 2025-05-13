using System;
using System.Collections.Concurrent;
using System.Data;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Bee.Define
{
    /// <summary>
    /// 自訂的 MessagePack 格式化器解析器，註冊 DataSet 和 DataTable 專用格式化器。
    /// </summary>
    public class TFormatterResolver : IFormatterResolver
    {
        /// <summary>
        /// Singleton 實例。
        /// </summary>
        public static readonly TFormatterResolver Instance = new TFormatterResolver();

        private TFormatterResolver() { }

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
                if (type == typeof(DataSet))
                    return new TDataSetFormatter();
                if (type == typeof(DataTable))
                    return new TDataTableFormatter();

                // fallback to standard resolver
                var method = typeof(StandardResolver).GetMethod("GetFormatter")
                    .MakeGenericMethod(type);
                return method.Invoke(StandardResolver.Instance, null);
            });
        }
    }
}
