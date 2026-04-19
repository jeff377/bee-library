namespace Bee.ObjectCaching.Runtime
{
    /// <summary>
    /// View state cache.
    /// </summary>
    internal class ViewStateCache : KeyObjectCache<object>
    {
        /// <summary>
        /// Gets the cache key for the specified member key.
        /// </summary>
        /// <param name="key">The member key.</param>
        protected override string GetCacheKey(string key)
        {
            return "ViewState_" + key;
        }

        /// <summary>
        /// Stores the view state in the cache.
        /// </summary>
        /// <param name="uniqueGUID">The page identifier.</param>
        /// <param name="viewState">The view state.</param>
        public void Set(Guid uniqueGUID, object viewState)
        {
            base.Set(uniqueGUID.ToString(), viewState);
        }

        /// <summary>
        /// Gets the view state from the cache.
        /// </summary>
        /// <param name="uniqueGUID">The page identifier.</param>
        public object? Get(Guid uniqueGUID)
        {
            return base.Get(uniqueGUID.ToString());
        }
    }
}
