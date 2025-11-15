using System;
using System.ComponentModel;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料庫項目集合。
    /// </summary>
    [Serializable]
    [Description("資料庫項目集合。")]
    [TreeNode("資料庫", true)]
    public class DatabaseItemCollection : KeyCollectionBase<DatabaseItem>
    {
    }
}
