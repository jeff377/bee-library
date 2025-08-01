using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 選單項目集合。
    /// </summary>
    [Serializable]
    [TreeNode("項目集合", false)]
    public class MenuItemCollection : KeyCollectionBase<MenuItem>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="folder">選單資料夾。</param>
        public MenuItemCollection(MenuFolder folder)
          : base(folder)
        { }

        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public MenuItem Add(string progId, string displayName)
        {
            var oItem = new MenuItem(progId, displayName);
            this.Add(oItem);
            return oItem;
        }
    }
}
