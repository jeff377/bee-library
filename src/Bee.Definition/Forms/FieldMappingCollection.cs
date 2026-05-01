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
        /// Adds a field mapping entry.
        /// </summary>
        /// <param name="sourceField">The source field.</param>
        /// <param name="destinationField">The destination field.</param>
        public FieldMapping Add(string sourceField, string destinationField)
        {
            var field = new FieldMapping(sourceField, destinationField);
            base.Add(field);
            return field;
        }

        /// <summary>
        /// Finds a mapping by its destination field name.
        /// </summary>
        /// <param name="destinationField">The destination field name.</param>
        public FieldMapping? FindByDestination(string destinationField)
        {
            return this.FirstOrDefault(m => StringUtilities.IsEquals(m.DestinationField, destinationField));
        }
    }
}
