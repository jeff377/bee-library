using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of menu nodes. Folders and entries share this one collection type and
    /// therefore one key space.
    /// </summary>
    [Description("Menu node collection.")]
    [TreeNode("Items", false)]
    public class MenuNodeCollection : KeyCollectionBase<MenuNodeBase>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MenuNodeCollection"/>.
        /// </summary>
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public MenuNodeCollection() : base()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="MenuNodeCollection"/>.
        /// </summary>
        /// <param name="owner">The owning <see cref="MenuSettings"/> or <see cref="MenuFolder"/>.</param>
        public MenuNodeCollection(object owner) : base(owner)
        { }

        /// <summary>
        /// Returns the visible nodes in display order (ascending <see cref="MenuNodeBase.Order"/>,
        /// ties keeping document order).
        /// </summary>
        /// <remarks>
        /// A shell builds its menu from this rather than from raw enumeration, so ordering and the
        /// <see cref="MenuNodeBase.Visible"/> switch are applied identically on every UI head. This
        /// applies the design-time switch only — per-user permission filtering is a separate
        /// concern the caller layers on top.
        /// </remarks>
        public IEnumerable<MenuNodeBase> GetDisplayNodes()
            => this.Where(node => node.Visible).OrderBy(node => node.Order);
    }

    /// <summary>
    /// Provides extension methods for <see cref="MenuNodeCollection"/>.
    /// </summary>
    /// <remarks>
    /// The convenience adds live here rather than on the collection itself because
    /// <see cref="KeyCollectionBase{T}"/> may expose only one public instance <c>Add</c>: the
    /// reflection-only XmlSerializer path used on AOT targets resolves a collection's add via
    /// <c>Type.GetMethod("Add")</c> and throws when more than one public overload exists.
    /// </remarks>
    public static class MenuNodeCollectionExtensions
    {
        /// <summary>
        /// Adds a folder to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="id">The node ID.</param>
        /// <param name="caption">The caption.</param>
        public static MenuFolder AddFolder(this MenuNodeCollection? collection, string id, string caption)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var folder = new MenuFolder(id, caption);
            collection.Add(folder);
            return folder;
        }

        /// <summary>
        /// Adds a program entry to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="id">The node ID.</param>
        /// <param name="progId">The program ID this entry opens.</param>
        /// <param name="caption">The caption.</param>
        public static MenuEntry AddEntry(this MenuNodeCollection? collection, string id, string progId, string caption)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var entry = new MenuEntry(id, progId, caption);
            collection.Add(entry);
            return entry;
        }
    }
}
