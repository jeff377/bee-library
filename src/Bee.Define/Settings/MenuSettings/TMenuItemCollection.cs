using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 選單項目集合。
    /// </summary>
    [Serializable]
    [TreeNode("項目集合", false)]
    public class TMenuItemCollection : TKeyCollectionBase<TMenuItem>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="folder">選單資料夾。</param>
        public TMenuItemCollection(TMenuFolder folder)
          : base(folder)
        { }

        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="progID">程式代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public TMenuItem Add(string progID, string displayName)
        {
            TMenuItem oItem;

            oItem = new TMenuItem(progID, displayName); ;
            this.Add(oItem);
            return oItem;
        }
    }
}
