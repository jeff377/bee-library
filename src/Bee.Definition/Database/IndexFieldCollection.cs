using Bee.Base;
using Bee.Base.Collections;
using System;

namespace Bee.Definition.Database
{
    /// <summary>
    /// Index field collection.
    /// </summary>
    [Serializable]
    public class IndexFieldCollection : KeyCollectionBase<IndexField>
    {
        /// <summary>
        /// Adds a new index field to the collection.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="sortDirection">The sort direction.</param>
        public IndexField Add(string fieldName, SortDirection sortDirection = SortDirection.Asc)
        {
            var indexFIeld = new IndexField(fieldName, sortDirection);
            this.Add(indexFIeld);
            return indexFIeld;
        }
    }
}
