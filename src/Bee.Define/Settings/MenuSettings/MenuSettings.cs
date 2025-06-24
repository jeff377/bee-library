using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Newtonsoft.Json;

namespace Bee.Define
{
    /// <summary>
    /// 選單設定。
    /// </summary>
    [Serializable]
    [XmlType("MenuSettings")]
    [Description("選單設定。")]
    [TreeNode]
    public class MenuSettings : IObjectSerializeFile, IDisplayName
    {
        private string _ObjectFilePath = string.Empty;
        private readonly DateTime _CreateInstanceTime = DateTime.MinValue;
        private SerializeState _SerializeState = SerializeState.None;
        private string _DisplayName = string.Empty;
        private MenuFolderCollection _Folders = null;
        private bool _IsLanguageLoaded = false;

        #region 建構函式

        /// <summary>
        /// 建構函式。
        /// </summary>
        public MenuSettings()
        {
            _CreateInstanceTime = DateTime.Now;
        }

        #endregion

        #region IObjectSerializeFile 介面

        /// <summary>
        /// 序列化狀態。
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState
        {
            get { return _SerializeState; }
        }

        /// <summary>
        /// 設定序列化狀態。
        /// </summary>
        /// <param name="serializeState">序列化狀態。</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            _SerializeState = serializeState;
            BaseFunc.SetSerializeState(_Folders, serializeState);
        }

        /// <summary>
        /// 物件執行個體的建立時間。
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public DateTime CreateInstanceTime
        {
            get { return _CreateInstanceTime; }
        }

        /// <summary>
        /// 序列化繫結檔案。
        /// </summary>
        [JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath
        {
            get { return _ObjectFilePath; }
        }

        /// <summary>
        /// 設定序列化/反序列化的對應檔案。
        /// </summary>
        /// <param name="fileName">檔案名稱。</param>
        public void SetObjectFilePath(string fileName)
        {
            _ObjectFilePath = fileName;
        }

        #endregion

        /// <summary>
        /// 顯示名稱。
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        [Description("顯示名稱。")]
        public virtual string DisplayName
        {
            get { return _DisplayName; }
            set { _DisplayName = value; }
        }

        /// <summary>
        /// 程式資料夾集合。
        /// </summary>
        [Description("程式資料夾集合。")]
        [DefaultValue(null)]
        public MenuFolderCollection Folders
        {
            get
            {
                // 序列化時，若集合無資料則傳回 null
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _Folders)) { return null; }
                if (_Folders == null) { _Folders = new MenuFolderCollection(this); }
                return _Folders;
            }
        }

        /// <summary>
        /// 執行階段屬性，是否已套用語系資料。
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        [JsonIgnore]
        public bool IsLanguageLoaded
        {
            get { return _IsLanguageLoaded; }
            set { _IsLanguageLoaded = value; }
        }

        /// <summary>
        /// 取得語系鍵值。
        /// </summary>
        /// <returns></returns>
        public virtual string GetLanguageKey()
        {
            return "MenuSettings";
        }

        /// <summary>
        /// 取得所有選單資料夾集合。
        /// </summary>
        /// <returns></returns>
        public List<MenuFolder> GetFolders()
        {
            List<MenuFolder> oFolders;

            oFolders = new List<MenuFolder>();
            foreach (MenuFolder folder in this.Folders)
                EnumFolders(folder, oFolders);
            return oFolders;
        }

        /// <summary>
        /// 由指定節點開始，列舉所有選單資料夾集合。
        /// </summary>
        /// <param name="folder">指定資料夾節點。</param>
        /// <param name="folders">資料夾集合。</param>
        private void EnumFolders(MenuFolder folder, List<MenuFolder> folders)
        {
            // 將本身資料夾加入集合
            folders.Add(folder);
            // 遞回往下層資料夾
            foreach (MenuFolder childFolder in folder.Folders)
                EnumFolders(childFolder, folders);
        }

        /// <summary>
        /// 取得所有選單項目集合。
        /// </summary>
        /// <returns></returns>
        public List<MenuItem> GetItems()
        {
            List<MenuItem> oItems;

            oItems = new List<MenuItem>();
            foreach (MenuFolder folder in this.Folders)
                Enumtems(folder, oItems);
            return oItems;
        }

        /// <summary>
        /// 由指定節點開始，列舉所有選單項目集合。
        /// </summary>
        /// <param name="folder">指定資料夾節點。</param>
        /// <param name="items">程式項目清單集合。</param>
        private void Enumtems(MenuFolder folder, List<MenuItem> items)
        {
            if (folder == null) return;
            // 列舉資料夾下的程式項目
            foreach (MenuItem item in folder.Items)
                items.Add(item);
            // 遞回往下層資料夾
            foreach (MenuFolder childFolder in folder.Folders)
                Enumtems(childFolder, items);
        }

        /// <summary>
        /// 尋找選單項目節點。
        /// </summary>
        /// <param name="progId">程式代碼。</param>
        /// <returns></returns>
        public MenuItem FindItem(string progId)
        {
            foreach (MenuFolder folder in this.Folders)
            {
                var item = folder.FindItem(progId);
                if (item != null)
                    return item;
            }
            return null;
        }
    }
}
