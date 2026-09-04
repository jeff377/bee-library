using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// Common base of every menu node. A node is either a <see cref="MenuFolder"/> (a grouping
    /// node that owns children) or a <see cref="MenuEntry"/> (a leaf pointing at one registered
    /// program).
    /// </summary>
    /// <remarks>
    /// Splitting folder from entry turns a validation rule into a type guarantee: a node carrying a
    /// program id structurally cannot own children, so "a program entry must be a leaf" needs no
    /// runtime check.
    /// <para>
    /// Both subclasses live in one collection and therefore share one key space, so <see cref="Id"/>
    /// must be unique across the whole tree rather than merely among siblings.
    /// <see cref="MenuSettings.Validate"/> enforces that.
    /// </para>
    /// </remarks>
    [Description("Menu node.")]
    [TreeNode]
    [XmlInclude(typeof(MenuFolder))]
    [XmlInclude(typeof(MenuEntry))]
    public abstract class MenuNodeBase : KeyCollectionItem
    {
        /// <summary>
        /// Gets or sets the node identifier, unique across the whole menu tree. Independent of
        /// <see cref="MenuEntry.ProgId"/> so a client can reference a node stably (deep links,
        /// recently used, favourites) even when several nodes open the same program.
        /// </summary>
        [XmlAttribute]
        [Description("Node ID (unique across the whole menu tree).")]
        public string Id
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// Gets or sets the caption shown to the user. This is the authoring-language original;
        /// translations live in the <c>Menu</c> language namespace and are keyed by
        /// <see cref="Id"/> rather than by this text.
        /// </summary>
        [XmlAttribute]
        [Description("Caption (authoring-language original; translations live in the Menu language namespace).")]
        [DefaultValue("")]
        public string Caption { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sort order among siblings; lower values come first. Nodes sharing an
        /// order keep their document order.
        /// </summary>
        [XmlAttribute]
        [Description("Sort order among siblings; lower comes first.")]
        [DefaultValue(0)]
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the icon identifier resolved by the consuming shell.
        /// </summary>
        [XmlAttribute]
        [Description("Icon identifier resolved by the consuming shell.")]
        [DefaultValue("")]
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the node appears in the menu at all.
        /// </summary>
        /// <remarks>
        /// WARNING: This is a design-time switch, not a permission mechanism. Its value is the same
        /// for every user of a deployment. Per-user visibility belongs to the permission layer
        /// (<see cref="PermissionModels"/>); using this flag to hide privileged functions would hide them
        /// from everyone and protect nothing.
        /// </remarks>
        [XmlAttribute]
        [Description("Whether the node appears in the menu (design-time switch, NOT a permission).")]
        [DefaultValue(true)]
        public bool Visible { get; set; } = true;
    }
}
