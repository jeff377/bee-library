using System;
using System.ComponentModel;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Define.Settings
{
    /// <summary>
    /// 資料庫伺服器集合。
    /// </summary>
    [Serializable]
    [Description("資料庫伺服器集合。")]
    [TreeNode("Servers", true)]
    public class DatabaseServerCollection : KeyCollectionBase<DatabaseServer>
    {
    }
}
