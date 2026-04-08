using System;
using System.ComponentModel;
using Bee.Core;
using Bee.Core.Attributes;
using Bee.Core.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of database items.
    /// </summary>
    [Serializable]
    [Description("Database item collection.")]
    [TreeNode("Databases", true)]
    public class DatabaseItemCollection : KeyCollectionBase<DatabaseItem>
    {
    }
}
