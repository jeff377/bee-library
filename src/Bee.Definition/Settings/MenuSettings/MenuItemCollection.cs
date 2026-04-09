using System;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of menu items.
    /// </summary>
    [Serializable]
    [TreeNode("Items", false)]
    public class MenuItemCollection : KeyCollectionBase<MenuItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MenuItemCollection"/>.
        /// </summary>
        /// <param name="folder">The owning menu folder.</param>
        public MenuItemCollection(MenuFolder folder)
          : base(folder)
        { }

        /// <summary>
        /// Adds a menu item to the collection.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        /// <param name="displayName">The display name.</param>
        public MenuItem Add(string progId, string displayName)
        {
            var oItem = new MenuItem(progId, displayName);
            this.Add(oItem);
            return oItem;
        }
    }
}
