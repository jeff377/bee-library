using System;
using System.ComponentModel;
using Bee.Core;
using Bee.Core.Attributes;
using Bee.Core.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of table items in a database schema.
    /// </summary>
    [Serializable]
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
