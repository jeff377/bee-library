using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A menu folder.
    /// </summary>
    [Serializable]
    [XmlType("MenuFolder")]
    [Description("Menu folder.")]
    [TreeNode("{0}", "DisplayName")]
    public class MenuFolder : KeyCollectionItem, IDisplayName
    {
        private MenuFolderCollection _folders = null;
        private MenuItemCollection _items = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="MenuFolder"/>.
        /// </summary>
        public MenuFolder()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="MenuFolder"/>.
        /// </summary>
        /// <param name="folderID">The folder ID.</param>
        /// <param name="displayName">The display name.</param>
        public MenuFolder(string folderID, string displayName)
        {
            FolderId = folderID;
            DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// Gets or sets the folder ID.
        /// </summary>
        [XmlAttribute]
        [Description("Folder ID.")]
        public string FolderId
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [XmlAttribute]
        [Description("Display name.")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the sub-folder collection.
        /// </summary>
        [Description("Sub-folder collection.")]
        [DefaultValue(null)]
        public MenuFolderCollection Folders
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _items)) { return null; }
                if (_items == null) { _folders = new MenuFolderCollection(this); }
                return _folders;
            }
        }

        /// <summary>
        /// Gets the menu item collection.
        /// </summary>
        [Description("Menu item collection.")]
        [DefaultValue(null)]
        public MenuItemCollection Items
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _items)) { return null; }
                if (_items == null) { _items = new MenuItemCollection(this); }
                return _items;
            }
        }

        /// <summary>
        /// Finds a menu item by program ID.
        /// </summary>
        /// <param name="progId">The program ID.</param>
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
        /// Gets the language key for this folder.
        /// </summary>
        /// <returns></returns>
        public string GetLanguageKey()
        {
            return StrFunc.Format("MenuFolder_{0}", this.FolderId);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return StrFunc.Format("{0} - {1}", this.FolderId, this.DisplayName);
        }
    }
}
