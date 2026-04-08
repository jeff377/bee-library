using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Core;
using Bee.Core.Attributes;
using Bee.Core.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A program item.
    /// </summary>
    [Serializable]
    [XmlType("ProgramItem")]
    [Description("Program item.")]
    [TreeNode]
    public class ProgramItem : KeyCollectionItem
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ProgramItem"/>.
        /// </summary>
        public ProgramItem()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ProgramItem"/>.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        /// <param name="displayName">The display name.</param>
        public ProgramItem(string progId, string displayName)
        {
            this.ProgId = progId;
            DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// Gets or sets the program ID.
        /// </summary>
        [XmlAttribute]
        [Description("Program ID.")]
        public string ProgId
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [XmlAttribute]
        [Description("Display name.")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.ProgId} - {this.DisplayName}";
        }
    }
}
