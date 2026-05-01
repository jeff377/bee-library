using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of table items in a database schema.
    /// </summary>
    [Description("Table item collection.")]
    [TreeNode("Tables", false)]
    public class TableItemCollection : KeyCollectionBase<TableItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TableItemCollection"/>.
        /// </summary>
        /// <param name="category">The owning database schema.</param>
        public TableItemCollection(DbSchema category) : base(category)
        { }
    }
}
