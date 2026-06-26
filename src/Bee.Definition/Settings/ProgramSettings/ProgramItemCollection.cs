using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of program items.
    /// </summary>
    [Description("Program item collection.")]
    [TreeNode("Program Items", false)]
    public class ProgramItemCollection : KeyCollectionBase<ProgramItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ProgramItemCollection"/>.
        /// </summary>
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public ProgramItemCollection() : base()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ProgramItemCollection"/>.
        /// </summary>
        /// <param name="category">The owning program category.</param>
        public ProgramItemCollection(ProgramCategory category) : base(category)
        { }
    }

    /// <summary>
    /// Provides extension methods for <see cref="ProgramItemCollection"/>.
    /// </summary>
    public static class ProgramItemCollectionExtensions
    {
        /// <summary>
        /// Adds a program item to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="progId">The program ID.</param>
        /// <param name="displayName">The display name.</param>
        public static ProgramItem Add(this ProgramItemCollection? collection, string progId, string displayName)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var item = new ProgramItem(progId, displayName);
            collection.Add(item);
            return item;
        }
    }
}
