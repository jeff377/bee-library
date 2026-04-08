namespace Bee.Core.Collections
{
    /// <summary>
    /// Interface for a strongly-typed keyed collection.
    /// </summary>
    public interface IKeyCollectionBase
    {
        /// <summary>
        /// Gets the owner of this collection.
        /// </summary>
        object Owner { get; }

        /// <summary>
        /// Changes the key of an existing item in the collection.
        /// </summary>
        /// <param name="key">The new key.</param>
        /// <param name="value">The item whose key should be changed.</param>
        void ChangeItemKey(string key, IKeyCollectionItem value);

        /// <summary>
        /// Removes the specified item from the collection.
        /// </summary>
        /// <param name="value">The item to remove.</param>
        void Remove(IKeyCollectionItem value);

        /// <summary>
        /// Adds the specified item to the collection.
        /// </summary>
        /// <param name="value">The item to add.</param>
        void Add(IKeyCollectionItem value);

        /// <summary>
        /// Inserts the specified item at the given index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the item.</param>
        /// <param name="value">The item to insert.</param>
        void Insert(int index, IKeyCollectionItem value);
    }
}
