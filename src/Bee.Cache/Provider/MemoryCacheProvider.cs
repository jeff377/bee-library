using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using Bee.Base;

namespace Bee.Cache
{
    /// <summary>
    /// 基於 MemoryCache 的快取提供者實作。
    /// </summary>
    public class MemoryCacheProvider : ICacheProvider
    {
        private readonly MemoryCache _memoryCache;

        /// <summary>
        /// 建構函式,使用預設的 MemoryCache。
        /// </summary>
        public MemoryCacheProvider()
        {
            _memoryCache = MemoryCache.Default;
        }

        /// <summary>
        /// 建構函式,使用指定的 MemoryCache。
        /// </summary>
        /// <param name="memoryCache">記憶體快取實例。</param>
        public MemoryCacheProvider(MemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// 取得不分大小寫的快取鍵值。
        /// </summary>
        /// <param name="key">原始鍵值。</param>
        private string GetCacheKey(string key)
        {
            return StrFunc.ToUpper(key);
        }

        /// <summary>
        /// 判斷快取項目是否存在於快取中。
        /// </summary>
        /// <param name="key">快取鍵值。</param>
        public bool Contains(string key)
        {
            string cacheKey = GetCacheKey(key);
            return _memoryCache.Contains(cacheKey);
        }

        /// <summary>
        /// 將快取項目置入快取區中。
        /// </summary>
        /// <param name="key">快取鍵值。</param>
        /// <param name="value">要置入快取的物件。</param>
        /// <param name="policy">快取項目到期條件。</param>
        public void Set(string key, object value, CacheItemPolicy policy)
        {
            var cacheKey = GetCacheKey(key);
            var cacheItem = new CacheItem(cacheKey, value);
            var cachePolicy = CacheFunc.CreateCachePolicy(policy);
            _memoryCache.Set(cacheItem, cachePolicy);
        }

        /// <summary>
        /// 從快取傳回項目。
        /// </summary>
        /// <param name="key">快取鍵值。</param>
        public object Get(string key)
        {
            string cacheKey = GetCacheKey(key);
            return _memoryCache.Get(cacheKey);
        }

        /// <summary>
        /// 移除快取項目。
        /// </summary>
        /// <param name="key">快取鍵值。</param>
        /// <returns>傳回移除的快取項目,若快取項目不存在則傳回 null。</returns>
        public object Remove(string key)
        {
            string cacheKey = GetCacheKey(key);
            return _memoryCache.Remove(cacheKey);
        }

        /// <summary>
        /// 從快取物件移除指定百分比的快取項目。
        /// </summary>
        /// <param name="percent">移除項目的數目在快取項目總數中所佔的百分比。</param>
        /// <returns>從快取區中移除的項目數量。</returns>
        public long Trim(int percent)
        {
            return _memoryCache.Trim(percent);
        }

        /// <summary>
        /// 傳回快取中的快取項目總數。
        /// </summary>
        public long GetCount()
        {
            return _memoryCache.GetCount();
        }

        /// <summary>
        /// 取得所有快取的鍵值清單。
        /// </summary>
        /// <returns>快取鍵值的字串列舉。</returns>
        public IEnumerable<string> GetAllKeys()
        {
            // MemoryCache 在列舉過程中可能被其他執行緒修改
            // 因此建議用 ToList() 先複製一份
            return _memoryCache.Select(item => item.Key).ToList();
        }
    }
}