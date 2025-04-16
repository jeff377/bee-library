using Bee.Base;

namespace Bee.Cache
{
    /// <summary>
    /// 物件快取基底類別。
    /// </summary>
    public abstract class TObjectCache<T>
    {
        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public TObjectCache()
        { }

        #endregion

        /// <summary>
        /// 取得快取項目到期條件。
        /// </summary>
        protected virtual TCacheItemPolicy GetPolicy()
        {
            TCacheItemPolicy oPolicy;

            // 預設為相對時間 20 分鐘
            oPolicy = new TCacheItemPolicy(ECacheTimeKind.SlidingTime, 20);
            return oPolicy;
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
            T oValue;
            string sKey;

            sKey = GetKey();

            // 若物件存在於快取區，則直接回傳該快取物件
            if (SysCache.Contains(sKey))               
                return (T)SysCache.Get(sKey);

            // 建立物件置入快取區，並回傳該物件
            oValue = CreateInstance();
            if (oValue != null)
            {
                SysCache.Set(sKey, oValue, GetPolicy());
            }
            return oValue;
        }

        /// <summary>
        /// 將物件置入快取中。
        /// </summary>
        /// <param name="value">要置入快取的物件。</param>
        public virtual void Set(T value)
        {
            string sKey;

            sKey = GetKey();
            SysCache.Set(sKey, value, GetPolicy());
        }

        /// <summary>
        /// 由快取區移除。
        /// </summary>
        public virtual void Remove()
        {
            string sKey;

            sKey = GetKey();
            SysCache.Remove(sKey);
        }
    }
}
