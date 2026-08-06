using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// The ordered plugin chain of one program. Declaration order is execution order.
    /// </summary>
    [Description("Plugin item collection.")]
    [TreeNode("Plugins", false)]
    public class PluginItemCollection : KeyCollectionBase<PluginItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PluginItemCollection"/>.
        /// </summary>
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public PluginItemCollection() : base()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="PluginItemCollection"/>.
        /// </summary>
        /// <param name="item">The owning program entry.</param>
        public PluginItemCollection(ProgramPluginItem item) : base(item)
        { }
    }

    /// <summary>
    /// Provides extension methods for <see cref="PluginItemCollection"/>.
    /// </summary>
    /// <remarks>
    /// The convenience overload lives here rather than on the collection because a collection
    /// serialized by the reflection-only <c>XmlSerializer</c> path may expose exactly one public
    /// instance <c>Add</c> (see <c>rules/apple-mobile-trim.md</c>).
    /// </remarks>
    public static class PluginItemCollectionExtensions
    {
        /// <summary>
        /// Adds a plugin binding to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="type">The assembly-qualified type name of the plugin.</param>
        public static PluginItem Add(this PluginItemCollection? collection, string type)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var item = new PluginItem(type);
            collection.Add(item);
            return item;
        }
    }
}
