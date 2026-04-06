using System;
using System.ComponentModel;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Define.Settings
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
