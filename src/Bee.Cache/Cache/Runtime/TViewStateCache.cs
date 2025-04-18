using System;

namespace Bee.Cache
{
    /// <summary>
    /// 頁面狀態快取。
    /// </summary>
    internal class TViewStateCache : TKeyObjectCache<object>
    {
        /// <summary>
        /// 取得快取鍵值。
        /// </summary>
        /// <param name="key">成員鍵值。</param>
        protected override string GetCacheKey(string key)
        {
            return "ViewState_" + key;
        }

        /// <summary>
        /// 將頁面狀態置入快取區。
        /// </summary>
        /// <param name="uniqueGUID">頁面識別。</param>
        /// <param name="viewState">頁面狀態。</param>
        public void Set(Guid uniqueGUID, object viewState)
        {
            base.Set(uniqueGUID.ToString(), viewState);
        }

        /// <summary>
        /// 由快取區取得頁面狀態。
        /// </summary>
        /// <param name="uniqueGUID">頁面識別。</param>
        public object Get(Guid uniqueGUID)
        {
            return base.Get(uniqueGUID.ToString());
        }
    }
}
