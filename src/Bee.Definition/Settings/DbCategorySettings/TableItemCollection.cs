using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of table items in a database category.
    /// </summary>
    [Description("Table item collection.")]
    [TreeNode("Tables", false)]
    public class TableItemCollection : KeyCollectionBase<TableItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TableItemCollection"/>.
        /// </summary>
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public TableItemCollection() : base()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="TableItemCollection"/>.
        /// </summary>
        /// <param name="category">The owning database category.</param>
        public TableItemCollection(DbCategory category) : base(category)
        { }
    }
}
