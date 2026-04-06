using System;
using System.ComponentModel;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Define.Settings
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
