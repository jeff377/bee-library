using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of per-program plugin chains, keyed by progId.
    /// </summary>
    [Description("Program plugin item collection.")]
    [TreeNode("Program Plugins", false)]
    public class ProgramPluginItemCollection : KeyCollectionBase<ProgramPluginItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ProgramPluginItemCollection"/>.
        /// </summary>
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public ProgramPluginItemCollection() : base()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ProgramPluginItemCollection"/>.
        /// </summary>
        /// <param name="settings">The owning plugin settings.</param>
        public ProgramPluginItemCollection(PluginSettings settings) : base(settings)
        { }
    }

    /// <summary>
    /// Provides extension methods for <see cref="ProgramPluginItemCollection"/>.
    /// </summary>
    /// <remarks>
    /// The convenience overload lives here rather than on the collection because a collection
    /// serialized by the reflection-only <c>XmlSerializer</c> path may expose exactly one public
    /// instance <c>Add</c> (see <c>rules/apple-mobile-trim.md</c>).
    /// </remarks>
    public static class ProgramPluginItemCollectionExtensions
    {
        /// <summary>
        /// Adds a program entry to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="progId">The program ID.</param>
        public static ProgramPluginItem Add(this ProgramPluginItemCollection? collection, string progId)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var item = new ProgramPluginItem(progId);
            collection.Add(item);
            return item;
        }
    }
}
