using System;
using System.ComponentModel;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A collection of program items.
    /// </summary>
    [Serializable]
    [Description("Program item collection.")]
    [TreeNode("Program Items", false)]
    public class ProgramItemCollection : KeyCollectionBase<ProgramItem>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ProgramItemCollection"/>.
        /// </summary>
        /// <param name="category">The owning program category.</param>
        public ProgramItemCollection(ProgramCategory category) : base(category)
        { }

        /// <summary>
        /// Adds a program item to the collection.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        /// <param name="displayName">The display name.</param>
        public ProgramItem Add(string progId, string displayName)
        {
            var item = new ProgramItem(progId, displayName);
            base.Add(item);
            return item;
        }
    }
}
