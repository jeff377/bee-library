using System;
using System.ComponentModel;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 資料表項目清單。
    /// </summary>
    [Serializable]
    [Description("資料表項目清單。")]
    [TreeNode("資料表", false)]
    public class DbTableItemCollection : KeyCollectionBase<DbTableItem>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="category">資料表分類。</param>
        public DbTableItemCollection(DbSchema category) : base(category)
        { }
    }
}
