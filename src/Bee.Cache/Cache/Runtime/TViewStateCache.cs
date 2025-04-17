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
    }
}
