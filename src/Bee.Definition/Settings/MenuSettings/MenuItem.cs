using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Core;
using Bee.Core.Attributes;
using Bee.Core.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A menu item.
    /// </summary>
    [Serializable]
    [XmlType("MenuItem")]
    [Description("Menu item.")]
    [TreeNode]
    public class MenuItem : KeyCollectionItem, IDisplayName
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="MenuItem"/>.
        /// </summary>
        public MenuItem()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        /// <param name="displayName">The display name.</param>
        public MenuItem(string progId, string displayName)
        {
            ProgId = progId;
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
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [XmlAttribute]
        [Description("Display name.")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Creates a copy of this instance.
        /// </summary>
        /// <returns></returns>
        public MenuItem Clone()
        {
            var item = new MenuItem();
            item.ProgId = this.ProgId;
            item.DisplayName = this.DisplayName;
            return item;
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return StrFunc.Format("{0} - {1}", this.ProgId, this.DisplayName);
        }
    }
}
