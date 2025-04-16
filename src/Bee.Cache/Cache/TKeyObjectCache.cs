using Bee.Base;

namespace Bee.Cache
{
    /// <summary>
    /// 透過鍵值存取同類型物件快取基底類別.。
    /// </summary>
    public abstract class TKeyObjectCache<T>
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TKeyObjectCache()
        { }

        #endregion

        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        protected virtual TCacheItemPolicy GetPolicy(string key)
        {
            TCacheItemPolicy oPolicy;

            // 預設為相對時間 20 分鐘
            oPolicy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            return oPolicy;
        }

        /// <summary>
        /// 取得快取鍵值。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        protected virtual string GetCacheKey(string key)
        {
            // 預設快取鍵值命名規則：型別名稱_成員鍵值
            // 註：型別名稱去除開頭的 T
            return StrFunc.LeftCut(typeof(T).Name, "T") + "_" + key;
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
            T oValue;
            string sKey;

            sKey = GetCacheKey(key);

            // 若物件存在於快取區，則直接回傳該快取物件
            if (SysCache.Contains(sKey))
                return (T)SysCache.Get(sKey);

            // 建立物件置入快取區，並回傳該物件
            oValue = CreateInstance(key);
            if (oValue != null)
            {
                SysCache.Set(sKey, oValue, GetPolicy(key));
            }
            return oValue;
        }

        /// <summary>
        /// 將物件置入快取中。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        /// <param name="value">要置入快取的物件。</param>
        public virtual void Set(string key, T value)
        {
            string sKey;

            sKey = GetCacheKey(key);
            SysCache.Set(sKey, value, GetPolicy(key));
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
                throw new TException("未實作 IKeyObject 介面");
        }

        /// <summary>
        /// 由快取區移除成員。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        public virtual void Remove(string key)
        {
            string sKey;

            sKey = GetCacheKey(key);
            SysCache.Remove(sKey);
        }
    }
}
