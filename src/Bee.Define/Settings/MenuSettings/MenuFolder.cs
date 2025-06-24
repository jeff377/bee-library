using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;

namespace Bee.Define
{
    /// <summary>
    /// 選單資料夾。
    /// </summary>
    [Serializable]
    [XmlType("MenuFolder")]
    [Description("選單資料夾。")]
    [TreeNode("{0}", "DisplayName")]
    public class MenuFolder : KeyCollectionItem, IDisplayName
    {
        private string _DisplayName = string.Empty;
        private MenuFolderCollection _Folders = null;
        private MenuItemCollection _Items = null;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public MenuFolder()
        { }

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="folderID">資料夾代碼。</param>
        /// <param name="displayName">顯示名稱。</param>
        public MenuFolder(string folderID, string displayName)
        {
            this.FolderID = folderID;
            _DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// 資料夾代碼。
        /// </summary>
        [XmlAttribute]
        [Description("資料夾代碼。")]
        public string FolderID
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlAttribute]
        [Description("顯示名稱。")]
        public string DisplayName
        {
            get { return _DisplayName; }
            set { _DisplayName = value; }
        }

        /// <summary>
        /// 子資料夾集合。
        /// </summary>
        [Description("子資料夾集合。")]
        [DefaultValue(null)]
        public MenuFolderCollection Folders
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Items)) { return null; }
                if (_Items == null) { _Folders = new MenuFolderCollection(this); }
                return _Folders;
            }
        }

        /// <summary>
        /// 選單項目集合。
        /// </summary>
        [Description("選單項目集合。")]
        [DefaultValue(null)]
        public MenuItemCollection Items
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Items)) { return null; }
                if (_Items == null) { _Items = new MenuItemCollection(this); }
                return _Items;
            }
        }

        /// <summary>
        /// 尋找選單項目節點。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        /// <returns></returns>
        public MenuItem FindItem(string progId)
        {
            foreach (MenuItem item in this.Items)
            {
                if (StrFunc.IsEquals(item.ProgId, progId))
                    return item;
            }

            foreach (MenuFolder folder in this.Folders)
            {
                var Item = folder.FindItem(progId);
                if (Item != null)
                    return Item;
            }

            return null;
        }

        /// <summary>
        /// 取得語系鍵值。
        /// </summary>
        /// <returns></returns>
        public string GetLanguageKey()
        {
            return StrFunc.Format("MenuFolder_{0}", this.FolderID);
        }

        /// <summary>
        /// 物件的描述文字。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return StrFunc.Format("{0} - {1}", this.FolderID, this.DisplayName);
        }
    }
}
