using System;
using System.ComponentModel;
using Bee.Core;
using Bee.Core.Attributes;
using Bee.Core.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of database server configurations.
    /// </summary>
    [Serializable]
    [Description("Database server collection.")]
    [TreeNode("Servers", true)]
    public class DatabaseServerCollection : KeyCollectionBase<DatabaseServer>
    {
    }
}
