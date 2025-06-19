using System;
using System.ComponentModel;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 程式項目集合。
    /// </summary>
    [Serializable]
    [Description("程式項目集合。")]
    [TreeNode("程式項目", false)]
    public class TProgramItemCollection : TKeyCollectionBase<TProgramItem>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="category">程式分類。</param>
        public TProgramItemCollection(TProgramCategory category) : base(category)
        { }

        /// <summary>
        /// 加入項目。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public TProgramItem Add(string progId, string displayName)
        {
            TProgramItem oItem;

            oItem = new TProgramItem(progId, displayName);
            base.Add(oItem);
            return oItem;
        }
    }
}
