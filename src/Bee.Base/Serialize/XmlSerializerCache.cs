using System;
using System.Collections.Concurrent;
using System.Xml.Serialization;

namespace Bee.Base
{
    /// <summary>
    /// 提供 <see cref="XmlSerializer"/> 快取功能，以提升序列化效能。
    /// </summary>
    public static class XmlSerializerCache
    {
        /// <summary>
        /// 快取已建立的 <see cref="XmlSerializer"/> 實例。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, XmlSerializer> _cache = new ConcurrentDictionary<Type, XmlSerializer>();

        /// <summary>
        /// 取得指定型別的 <see cref="XmlSerializer"/> 實例，若尚未建立則自動加入快取。
        /// </summary>
        /// <param name="type">目標序列化型別。</param>
        /// <returns>對應型別的 <see cref="XmlSerializer"/> 實例。</returns>
        public static XmlSerializer Get(Type type)
        {
            return _cache.GetOrAdd(type, t => new XmlSerializer(t));
        }
    }

}
