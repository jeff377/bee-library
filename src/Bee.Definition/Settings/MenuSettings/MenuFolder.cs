using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Serialization;

namespace Bee.Definition.Settings
{
    /// <summary>
    /// A grouping node. Owns child nodes and references no program of its own.
    /// </summary>
    [Description("Menu folder (grouping node).")]
    [TreeNode]
    public class MenuFolder : MenuNodeBase
    {
        private MenuNodeCollection? _items = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="MenuFolder"/>.
        /// </summary>
        public MenuFolder()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="MenuFolder"/>.
        /// </summary>
        /// <param name="id">The node ID.</param>
        /// <param name="caption">The caption.</param>
        public MenuFolder(string id, string caption)
        {
            Id = id;
            Caption = caption;
        }

        #endregion

        /// <summary>
        /// Gets the child node collection.
        /// </summary>
        /// <remarks>
        /// Each subtype is declared with its own <see cref="XmlArrayItemAttribute"/> so the
        /// serializer writes <c>&lt;MenuFolder&gt;</c> and <c>&lt;MenuEntry&gt;</c> elements.
        /// Without the per-subtype declarations it falls back to one element name plus an
        /// <c>xsi:type</c> discriminator, which is markedly harder to read and to hand-edit.
        /// </remarks>
        [Description("Child node collection.")]
        [Browsable(false)]
        [DefaultValue(null)]
        [XmlArrayItem(typeof(MenuFolder))]
        [XmlArrayItem(typeof(MenuEntry))]
        public MenuNodeCollection? Items
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(this.SerializeState, _items!)) { return null; }
                if (_items == null) { _items = new MenuNodeCollection(this); }
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
            return $"{this.Id} - {this.Caption}";
        }
    }
}
