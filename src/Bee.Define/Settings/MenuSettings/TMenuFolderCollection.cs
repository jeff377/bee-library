using System;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 選單資料夾集合。
    /// </summary>
    [Serializable]
    [TreeNode("資料夾集合", false)]
    public class TMenuFolderCollection : TKeyCollectionBase<TMenuFolder>
    {
        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="settings">選單設定。</param>
        public TMenuFolderCollection(TMenuSettings settings) : base(settings)
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="folder">選單資料夾。</param>
        public TMenuFolderCollection(TMenuFolder folder) : base(folder)
        { }

        /// <summary>
        /// 加入成員。
        /// </summary>
        /// <param name="folderID">資料夾代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public TMenuFolder Add(string folderID, string displayName)
        {
            TMenuFolder oFolder;

            oFolder = new TMenuFolder(folderID, displayName);
            this.Add(oFolder);
            return oFolder;
        }
    }
}
