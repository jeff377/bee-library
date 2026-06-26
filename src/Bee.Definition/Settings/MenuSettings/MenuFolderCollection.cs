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
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public MenuFolderCollection() : base()
        { }

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
    }

    /// <summary>
    /// Provides extension methods for <see cref="MenuFolderCollection"/>.
    /// </summary>
    public static class MenuFolderCollectionExtensions
    {
        /// <summary>
        /// Adds a folder to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="folderID">The folder ID.</param>
        /// <param name="displayName">The display name.</param>
        public static MenuFolder Add(this MenuFolderCollection? collection, string folderID, string displayName)
        {
            ArgumentNullException.ThrowIfNull(collection);
            MenuFolder oFolder;

            oFolder = new MenuFolder(folderID, displayName);
            collection.Add(oFolder);
            return oFolder;
        }
    }
}
