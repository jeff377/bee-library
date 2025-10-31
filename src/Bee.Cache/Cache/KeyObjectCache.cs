using System;
using Bee.Base;

namespace Bee.Cache
{
    /// <summary>
    /// 透過鍵值存取同類型物件快取基底類別.。
    /// </summary>
    public abstract class KeyObjectCache<T>
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public KeyObjectCache()
        { }

        #endregion

        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        protected virtual CacheItemPolicy GetPolicy(string key)
        {
            // 預設為相對時間 20 分鐘
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            return policy;
        }

        /// <summary>
        /// 取得快取鍵值，統一轉小寫以避免大小寫差異。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        protected virtual string GetCacheKey(string key)
        {
            return (typeof(T).Name + "_" + key).ToLowerInvariant();
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        protected virtual T CreateInstance(string key)
        {
            return default;
        }

        /// <summary>
        /// 依成員鍵值取得物件。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        public virtual T Get(string key)
        {
            // 取得快取鍵值
            string cacheKey = GetCacheKey(key);
            // 若物件存在於快取區，則直接回傳該快取物件
            if (CacheInfo.CacheProvider.Contains(cacheKey))
                return (T)CacheInfo.CacheProvider.Get(cacheKey);

            // 建立物件置入快取區，並回傳該物件
            var value = CreateInstance(key);
            if (value != null)
            {
                CacheInfo.CacheProvider.Set(cacheKey, value, GetPolicy(key));
            }
            return value;
        }

        /// <summary>
        /// 將物件置入快取中。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        /// <param name="value">要置入快取的物件。</param>
        public virtual void Set(string key, T value)
        {
            string cacheKey = GetCacheKey(key);
            CacheInfo.CacheProvider.Set(cacheKey, value, GetPolicy(key));
        }

        /// <summary>
        /// 將物件置入快取中，物件需具有 IKeyObject 介面才能取得成員鍵值。
        /// </summary>
        /// <param name="value">要置入快取的物件。</param>
        public virtual void Set(T value)
        {
            if (value is IKeyObject c)
                Set(c.GetKey(), value);
            else
                throw new InvalidOperationException("The value does not implement the IKeyObject interface.");
        }

        /// <summary>
        /// 由快取區移除成員。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        public virtual void Remove(string key)
        {
            string cacheKey = GetCacheKey(key);
            CacheInfo.CacheProvider.Remove(cacheKey);
        }
    }
}
