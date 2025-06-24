using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 選單資料夾集合。
    /// </summary>
    [Serializable]
    [TreeNode("資料夾集合", false)]
    public class MenuFolderCollection : KeyCollectionBase<MenuFolder>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="settings">選單設定。</param>
        public MenuFolderCollection(MenuSettings settings) : base(settings)
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="folder">選單資料夾。</param>
        public MenuFolderCollection(MenuFolder folder) : base(folder)
        { }

        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="folderID">資料夾代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public MenuFolder Add(string folderID, string displayName)
        {
            MenuFolder oFolder;

            oFolder = new MenuFolder(folderID, displayName);
            this.Add(oFolder);
            return oFolder;
        }
    }
}
