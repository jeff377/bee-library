using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using System.Text.Json.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Menu settings.
    /// </summary>
    [Serializable]
    [XmlType("MenuSettings")]
    [Description("Menu settings.")]
    [TreeNode]
    public class MenuSettings : IObjectSerializeFile, IDisplayName
    {
        private MenuFolderCollection? _folders = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="MenuSettings"/>.
        /// </summary>
        public MenuSettings()
        {
        }

        #endregion

        #region IObjectSerializeFile Interface

        /// <summary>
        /// Gets the serialization state.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            BaseFunc.SetSerializeState(_folders!, serializeState);
        }

        /// <summary>
        /// Gets the file path bound to serialization.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// Sets the file path bound for serialization/deserialization.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public void SetObjectFilePath(string filePath)
        {
            ObjectFilePath = filePath;
        }

        #endregion

        /// <summary>
        /// Gets the time at which this object was created.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        [Description("Display name.")]
        public virtual string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the program folder collection.
        /// </summary>
        [Description("Program folder collection.")]
        [DefaultValue(null)]
        public MenuFolderCollection? Folders
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(this.SerializeState, _folders!)) { return null; }
                if (_folders == null) { _folders = new MenuFolderCollection(this); }
                return _folders;
            }
        }

        /// <summary>
        /// Gets all menu folders as a flat list.
        /// </summary>
        /// <returns></returns>
        public List<MenuFolder> GetFolders()
        {
            List<MenuFolder> oFolders;

            oFolders = [];
            foreach (MenuFolder folder in this.Folders!)
                EnumFolders(folder, oFolders);
            return oFolders;
        }

        /// <summary>
        /// Recursively enumerates all menu folders starting from the specified node.
        /// </summary>
        /// <param name="folder">The starting folder node.</param>
        /// <param name="folders">The folder collection to populate.</param>
        private static void EnumFolders(MenuFolder folder, List<MenuFolder> folders)
        {
            // Add this folder to the collection
            folders.Add(folder);
            // Recurse into child folders
            foreach (MenuFolder childFolder in folder.Folders!)
                EnumFolders(childFolder, folders);
        }

        /// <summary>
        /// Gets all menu items as a flat list.
        /// </summary>
        /// <returns></returns>
        public List<MenuItem> GetItems()
        {
            List<MenuItem> oItems;

            oItems = [];
            foreach (MenuFolder folder in this.Folders!)
                Enumtems(folder, oItems);
            return oItems;
        }

        /// <summary>
        /// Recursively enumerates all menu items starting from the specified node.
        /// </summary>
        /// <param name="folder">The starting folder node.</param>
        /// <param name="items">The item list to populate.</param>
        private static void Enumtems(MenuFolder folder, List<MenuItem> items)
        {
            if (folder == null) return;
            // Enumerate program items under this folder
            foreach (MenuItem item in folder.Items!)
                items.Add(item);
            // Recurse into child folders
            foreach (MenuFolder childFolder in folder.Folders!)
                Enumtems(childFolder, items);
        }

        /// <summary>
        /// Finds a menu item by program ID.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        /// <returns></returns>
        public MenuItem? FindItem(string progId)
        {
            foreach (MenuFolder folder in this.Folders!)
            {
                var item = folder.FindItem(progId);
                if (item != null)
                    return item;
            }
            return null;
        }
    }
}
