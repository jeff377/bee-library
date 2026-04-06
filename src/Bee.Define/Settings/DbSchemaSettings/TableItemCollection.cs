using System;
using System.ComponentModel;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Define.Settings
{
    /// <summary>
    /// 資料表項目清單。
    /// </summary>
    [Serializable]
    [Description("資料表項目清單。")]
    [TreeNode("資料表", false)]
    public class TableItemCollection : KeyCollectionBase<TableItem>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="category">資料表分類。</param>
        public TableItemCollection(DbSchema category) : base(category)
        { }
    }
}
