using System.Runtime.Caching;
using Bee.Base;

namespace Bee.Cache
{
    /// <summary>
    /// 系統快取操作類別，使用 MemoryCache 進行快取。
    /// </summary>
    public static class SysCache
    {
        /// <summary>
        /// 記憶體快取。
        /// </summary>
        private static MemoryCache MemoryCache
        {
            get { return MemoryCache.Default; }
        }

        /// <summary>
        /// 取得不分大小寫的快取鍵值。
        /// </summary>
        /// <param name="key">原始鍵值。</param>
        private static string GetKey(string key)
        {
            return StrFunc.ToUpper(key);
        }

        /// <summary>
        /// 判斷快取項目是否存在於快取中。
        /// </summary>
        /// <param name="key">快取鍵值。</param>
        public static bool Contains(string key)
        {
            string sKey;

            sKey = GetKey(key);
            return MemoryCache.Contains(sKey);
        }

        /// <summary>
        /// 將快取項目置入快取區中。
        /// </summary>
        /// <param name="item">快取項目。</param>
        /// <param name="policy">快取項目到期條件。</param>
        /// <remarks>如果快取資料不存在，則會建立它。 如果快取資料存在，則會更新，</remarks>
        public static void Set(CacheItem item, CacheItemPolicy policy)
        {
            item.Key = GetKey(item.Key);
            MemoryCache.Set(item, policy);
        }

        /// <summary>
        /// 將快取項目置入快取區中。
        /// </summary>
        /// <param name="key">快取鍵值。</param>
        /// <param name="value">要置入快取的物件。</param>
        /// <param name="policy">快取項目到期條件。</param>
        /// <remarks>如果快取資料不存在，則會建立它。 如果快取資料存在，則會更新，</remarks>
        public static void Set(string key, object value, TCacheItemPolicy policy)
        {
            CacheItem oItem;
            CacheItemPolicy oPolicy;

            oItem = new CacheItem(key, value);
            oPolicy = CacheFunc.CreateCachePolicy(policy);
            Set(oItem, oPolicy);
        }

        /// <summary>
        /// 從快取傳回項目。
        /// </summary>
        /// <param name="key">快取鍵值。</param>
        public static object Get(string key)
        {
            string sKey;

            sKey = GetKey(key);
            return MemoryCache.Get(sKey);
        }

        /// <summary>
        /// 移除快取項目。
        /// </summary>
        /// <param name="key">快取鍵值。</param>
        /// <returns>傳回移除的快取項目，若快取項目不存在則傳回 null。</returns>
        public static object Remove(string key)
        {
            string sKey;

            sKey = GetKey(key);
            return MemoryCache.Remove(sKey);
        }

        /// <summary>
        /// 從快取物件移除指定百分比的快取項目。
        /// </summary>
        /// <param name="percent">移除項目的數目在快取項目總數中所佔的百分比。</param>
        /// <returns>從快取區中移除的項目數量。</returns>
        public static long Trim(int percent)
        {
            return MemoryCache.Trim(percent);
        }

        /// <summary>
        /// 清除所有快取。
        /// </summary>
        /// <returns>從快取區中移除的項目數量。</returns>
        public static long Clear()
        {
            return Trim(100);
        }

        /// <summary>
        /// 傳回快取中的快取項目總數。
        /// </summary>
        public static long GetCount()
        {
            return MemoryCache.GetCount();
        }
    }
}
