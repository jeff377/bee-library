using Bee.Base;
using Bee.Base.Collections;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// A collection of field mappings.
    /// </summary>
    public class FieldMappingCollection : CollectionBase<FieldMapping>
    {
        /// <summary>
        /// Finds a mapping by its destination field name.
        /// </summary>
        /// <param name="destinationField">The destination field name.</param>
        public FieldMapping? FindByDestination(string destinationField)
        {
            return this.FirstOrDefault(m => StringUtilities.IsEquals(m.DestinationField, destinationField));
        }
    }

    /// <summary>
    /// Convenience extension methods for <see cref="FieldMappingCollection"/>.
    /// </summary>
    public static class FieldMappingCollectionExtensions
    {
        /// <summary>
        /// Adds a field mapping entry.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="sourceField">The source field.</param>
        /// <param name="destinationField">The destination field.</param>
        public static FieldMapping Add(this FieldMappingCollection? collection, string sourceField, string destinationField)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var field = new FieldMapping(sourceField, destinationField);
            collection.Add(field);
            return field;
        }
    }
}
