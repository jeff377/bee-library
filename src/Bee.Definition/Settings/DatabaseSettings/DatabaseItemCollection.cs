using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of database items.
    /// </summary>
    [Description("Database item collection.")]
    [TreeNode("Databases", true)]
    public class DatabaseItemCollection : KeyCollectionBase<DatabaseItem>
    {
    }
}
