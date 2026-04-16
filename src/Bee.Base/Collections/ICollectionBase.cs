namespace Bee.Base.Collections
{
    /// <summary>
    /// Interface for strongly-typed collections.
    /// </summary>
    public interface ICollectionBase
    {
        /// <summary>
        /// Gets the owner of this collection.
        /// </summary>
        object? Owner { get; }

        /// <summary>
        /// Removes the specified item from the collection.
        /// </summary>
        /// <param name="value">The item to remove.</param>
        void Remove(ICollectionItem value);

        /// <summary>
        /// Adds the specified item to the collection.
        /// </summary>
        /// <param name="value">The item to add.</param>
        void Add(ICollectionItem value);

        /// <summary>
        /// Inserts the specified item at the given index.
        /// </summary>
        /// <param name="index">The zero-based index at which to insert the item.</param>
        /// <param name="value">The item to insert.</param>
        void Insert(int index, ICollectionItem value);
    }
}
