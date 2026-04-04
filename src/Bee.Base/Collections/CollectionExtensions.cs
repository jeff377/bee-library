using System.Data;

namespace Bee.Base.Collections
{
    /// <summary>
    /// 集合擴充方法。
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// 取得屬性值。
        /// </summary>
        /// <typeparam name="T">泛型型別。</typeparam>
        /// <param name="collection">屬性集合。</param>
        /// <param name="key">屬性健值。</param>
        /// <param name="defaultValue">預設值。</param>
        public static T GetValue<T>(this PropertyCollection collection, string key, object defaultValue)
        {
            if (collection.Contains(key))
                return (T)collection[key];
            else
                return (T)defaultValue;
        }
    }
}
