using System;
using System.ComponentModel;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Define
{
    /// <summary>
    /// 資料庫項目集合。
    /// </summary>
    [Serializable]
    [Description("資料庫項目集合。")]
    [TreeNode("Databases", true)]
    public class DatabaseItemCollection : KeyCollectionBase<DatabaseItem>
    {
    }
}
