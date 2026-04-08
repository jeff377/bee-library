using System;
using System.ComponentModel;
using Bee.Core;
using Bee.Core.Attributes;
using Bee.Core.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of program categories.
    /// </summary>
    [Serializable]
    [Description("Program category collection.")]
    [TreeNode("Categories", false)]
    public class ProgramCategoryCollection : KeyCollectionBase<ProgramCategory>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ProgramCategoryCollection"/>.
        /// </summary>
        /// <param name="settings">The owning program settings.</param>
        public ProgramCategoryCollection(ProgramSettings settings) : base(settings)
        { }

        /// <summary>
        /// Adds a category to the collection.
        /// </summary>
        /// <param name="id">The category ID.</param>
        /// <param name="displayName">The display name.</param>
        public ProgramCategory Add(string id, string displayName)
        {
            var category = new ProgramCategory(id, displayName);
            base.Add(category);
            return category;
        }
    }
}
