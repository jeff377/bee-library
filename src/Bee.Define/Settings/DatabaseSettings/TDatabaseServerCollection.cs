using System;
using System.ComponentModel;
using Bee.Base;


namespace Bee.Define
{
    /// <summary>
    /// 資料庫伺服器集合。
    /// </summary>
    [Serializable]
    [Description("資料庫伺服器集合。")]
    [TreeNode("伺服器", false)]
    public class TDatabaseServerCollection : TKeyCollectionBase<TDatabaseServer>
    {
    }
}
