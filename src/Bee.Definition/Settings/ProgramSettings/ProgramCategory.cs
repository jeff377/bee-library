using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using Bee.Base.Collections;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A program category.
    /// </summary>
    [XmlType("ProgramCategory")]
    [Description("Program category.")]
    [TreeNode]
    public class ProgramCategory : KeyCollectionItem
    {
        private ProgramItemCollection? _items = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="ProgramCategory"/>.
        /// </summary>
        public ProgramCategory()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="ProgramCategory"/>.
        /// </summary>
        /// <param name="id">The category ID.</param>
        /// <param name="displayName">The display name.</param>
        public ProgramCategory(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// Gets or sets the category ID.
        /// </summary>
        [XmlAttribute]
        [Description("Category ID.")]
        public string Id
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
        /// Gets the program item collection.
        /// </summary>
        [Description("Program item collection.")]
        [Browsable(false)]
        [DefaultValue(null)]
        public ProgramItemCollection? Items
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(this.SerializeState, _items!)) { return null; }
                if (_items == null) { _items = new ProgramItemCollection(this); }
                return _items;
            }
        }

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            _items?.SetSerializeState(serializeState);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{this.Id} - {this.DisplayName}";
        }
    }
}
