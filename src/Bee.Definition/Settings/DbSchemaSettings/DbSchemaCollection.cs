using System;
using System.ComponentModel;
using Bee.Core;
using Bee.Core.Attributes;
using Bee.Core.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of database schema definitions.
    /// </summary>
    [Serializable]
    [Description("Database schema collection.")]
    [TreeNode("Database Schemas", false)]
    public class DbSchemaCollection : KeyCollectionBase<DbSchema>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DbSchemaCollection"/>.
        /// </summary>
        /// <param name="settings">The owning database schema settings.</param>
        public DbSchemaCollection(DbSchemaSettings settings) : base(settings)
        { }
    }
}
