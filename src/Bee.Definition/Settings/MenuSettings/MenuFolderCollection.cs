using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of menu folders.
    /// </summary>
    [TreeNode("Folders", false)]
    public class MenuFolderCollection : KeyCollectionBase<MenuFolder>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MenuFolderCollection"/>.
        /// </summary>
        /// <param name="settings">The owning menu settings.</param>
        public MenuFolderCollection(MenuSettings settings) : base(settings)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="MenuFolderCollection"/>.
        /// </summary>
        /// <param name="folder">The owning menu folder.</param>
        public MenuFolderCollection(MenuFolder folder) : base(folder)
        { }

        /// <summary>
        /// Adds a folder to the collection.
        /// </summary>
        /// <param name="folderID">The folder ID.</param>
        /// <param name="displayName">The display name.</param>
        public MenuFolder Add(string folderID, string displayName)
        {
            MenuFolder oFolder;

            oFolder = new MenuFolder(folderID, displayName);
            this.Add(oFolder);
            return oFolder;
        }
    }
}
