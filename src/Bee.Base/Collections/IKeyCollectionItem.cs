namespace Bee.Base.Collections
{
    /// <summary>
    /// Interface for an item in a strongly-typed keyed collection.
    /// </summary>
    public interface IKeyCollectionItem
    {
        /// <summary>
        /// Gets or sets the key of this item.
        /// </summary>
        string Key { get; set; }

        /// <summary>
        /// Sets the collection that owns this item.
        /// </summary>
        /// <param name="collection">The owning collection.</param>
        void SetCollection(IKeyCollectionBase collection);

        /// <summary>
        /// Removes this item from its owning collection.
        /// </summary>
        void Remove();
    }
}
