using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of program categories.
    /// </summary>
    [Description("Program category collection.")]
    [TreeNode("Categories", false)]
    public class ProgramCategoryCollection : KeyCollectionBase<ProgramCategory>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ProgramCategoryCollection"/>.
        /// </summary>
        /// <remarks>
        /// Required by XmlSerializer's reflection-only deserialization path (AOT targets such as iOS
        /// create the collection via the public parameterless constructor).
        /// </remarks>
        public ProgramCategoryCollection() : base()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ProgramCategoryCollection"/>.
        /// </summary>
        /// <param name="settings">The owning program settings.</param>
        public ProgramCategoryCollection(ProgramSettings settings) : base(settings)
        { }
    }

    /// <summary>
    /// Provides extension methods for <see cref="ProgramCategoryCollection"/>.
    /// </summary>
    public static class ProgramCategoryCollectionExtensions
    {
        /// <summary>
        /// Adds a category to the collection.
        /// </summary>
        /// <param name="collection">The collection to add to.</param>
        /// <param name="id">The category ID.</param>
        /// <param name="displayName">The display name.</param>
        public static ProgramCategory Add(this ProgramCategoryCollection? collection, string id, string displayName)
        {
            ArgumentNullException.ThrowIfNull(collection);
            var category = new ProgramCategory(id, displayName);
            collection.Add(category);
            return category;
        }
    }
}
