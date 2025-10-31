namespace Bee.Cache
{
    /// <summary>
    /// 物件快取基底類別。
    /// </summary>
    public abstract class ObjectCache<T>
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public ObjectCache()
        { }

        #endregion

        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        protected virtual CacheItemPolicy GetPolicy()
        {
            // 預設為相對時間 20 分鐘
            var policy = new CacheItemPolicy(CacheTimeKind.SlidingTime, 20);
            return policy;
        }

        /// <summary>
        /// 取得快取鍵值。
        /// </summary>
        protected virtual string GetKey()
        {
            return typeof(T).Name;
        }

        /// <summary>
        /// 建立執行個體。
        /// </summary>
        protected virtual T CreateInstance()
        {
            return default;
        }

        /// <summary>
        /// 取得物件。
        /// </summary>
        public virtual T Get()
        {
            // 取得快取鍵值
            string key = GetKey();
            // 若物件存在於快取區，則直接回傳該快取物件
            if (CacheInfo.CacheProvider.Contains(key))               
                return (T)CacheInfo.CacheProvider.Get(key);

            // 建立物件置入快取區，並回傳該物件
            var value = CreateInstance();
            if (value != null)
            {
                CacheInfo.CacheProvider.Set(key, value, GetPolicy());
            }
            return value;
        }

        /// <summary>
        /// 將物件置入快取中。
        /// </summary>
        /// <param name="value">要置入快取的物件。</param>
        public virtual void Set(T value)
        {
            string key = GetKey();
            CacheInfo.CacheProvider.Set(key, value, GetPolicy());
        }

        /// <summary>
        /// 由快取區移除。
        /// </summary>
        public virtual void Remove()
        {
            string key = GetKey();
            CacheInfo.CacheProvider.Remove(key);
        }
    }
}
