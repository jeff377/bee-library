using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of menu items.
    /// </summary>
    [TreeNode("Items", false)]
    public class MenuItemCollection : KeyCollectionBase<MenuItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MenuItemCollection"/>.
        /// </summary>
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public MenuItemCollection() : base()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="MenuItemCollection"/>.
        /// </summary>
        /// <param name="folder">The owning menu folder.</param>
        public MenuItemCollection(MenuFolder folder)
          : base(folder)
        { }
    }

    /// <summary>
    /// Provides extension methods for <see cref="MenuItemCollection"/>.
    /// </summary>
    public static class MenuItemCollectionExtensions
    {
        /// <summary>
        /// Adds a menu item to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="progId">The program ID.</param>
        /// <param name="displayName">The display name.</param>
        public static MenuItem Add(this MenuItemCollection? collection, string progId, string displayName)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var oItem = new MenuItem(progId, displayName);
            collection.Add(oItem);
            return oItem;
        }
    }
}
