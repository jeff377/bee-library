using System;
using System.ComponentModel;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 程式分類集合。
    /// </summary>
    [Serializable]
    [Description("程式分類集合。")]
    [TreeNode("分類", false)]
    public class TProgramCategoryCollection : TKeyCollectionBase<TProgramCategory>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="settings">程式清單。</param>
        public TProgramCategoryCollection(TProgramSettings settings) : base(settings)
        { }

        /// <summary>
        /// 加入分類。
        /// </summary>
        /// <param name="id">分類代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public TProgramCategory Add(string id, string displayName)
        {
            TProgramCategory oCategory;

            oCategory = new TProgramCategory(id, displayName);
            base.Add(oCategory);
            return oCategory;
        }
    }
}
