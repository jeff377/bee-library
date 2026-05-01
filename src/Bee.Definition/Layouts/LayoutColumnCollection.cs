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
        /// <summary>
        /// Adds a column to the collection.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="caption">The caption text.</param>
        /// <param name="controlType">The control type.</param>
        public LayoutColumn Add(string fieldName, string caption, ColumnControlType controlType)
        {
            var column = new LayoutColumn(fieldName, caption, controlType);
            this.Add(column);
            return column;
        }
    }
}
