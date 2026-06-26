using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A collection of grid layout columns.
    /// </summary>
    [Description("Grid layout column collection.")]
    [TreeNode("Columns", false)]
    public class LayoutColumnCollection : CollectionBase<LayoutColumn>
    {
    }

    /// <summary>
    /// Extension methods for <see cref="LayoutColumnCollection"/>.
    /// </summary>
    public static class LayoutColumnCollectionExtensions
    {
        /// <summary>
        /// Adds a column to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="caption">The caption text.</param>
        /// <param name="controlType">The control type.</param>
        public static LayoutColumn Add(this LayoutColumnCollection? collection, string fieldName, string caption, ControlType controlType)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var column = new LayoutColumn(fieldName, caption, controlType);
            collection.Add(column);
            return column;
        }
    }
}
