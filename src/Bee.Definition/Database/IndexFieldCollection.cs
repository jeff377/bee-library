using Bee.Base.Collections;
using Bee.Definition.Sorting;

namespace Bee.Definition.Database
{
    /// <summary>
    /// Index field collection.
    /// </summary>
    public class IndexFieldCollection : KeyCollectionBase<IndexField>
    {
    }

    /// <summary>
    /// Extension methods for <see cref="IndexFieldCollection"/>.
    /// </summary>
    public static class IndexFieldCollectionExtensions
    {
        /// <summary>
        /// Adds a new index field to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="sortDirection">The sort direction.</param>
        public static IndexField Add(this IndexFieldCollection? collection, string fieldName, SortDirection sortDirection = SortDirection.Asc)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var indexFIeld = new IndexField(fieldName, sortDirection);
            collection.Add(indexFIeld);
            return indexFIeld;
        }
    }
}
