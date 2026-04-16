namespace Bee.Base.Collections
{
    /// <summary>
    /// Interface for strongly-typed collection items.
    /// </summary>
    public interface ICollectionItem
    {
        /// <summary>
        /// Sets the collection that owns this item.
        /// </summary>
        /// <param name="collection">The owning collection.</param>
        void SetCollection(ICollectionBase? collection);

        /// <summary>
        /// Removes this item from its owning collection.
        /// </summary>
        void Remove();
    }
}
