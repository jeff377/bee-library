using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Collections;
using Bee.Base.Serialization;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// A section in the master area of a <see cref="FormLayout"/>.
    /// All sections share the column division defined by <see cref="FormLayout.ColumnCount"/>.
    /// </summary>
    [Description("Layout section.")]
    [TreeNode]
    public class LayoutSection : CollectionItem
    {
        private LayoutFieldCollection? _fields = null;

        /// <summary>
        /// Gets or sets the section name.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Section name.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the caption text.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Caption text.")]
        [DefaultValue("")]
        public string Caption { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the caption is shown.
        /// </summary>
        [XmlAttribute]
        [Description("Indicates whether the caption is shown.")]
        [DefaultValue(true)]
        public bool ShowCaption { get; set; } = true;

        /// <summary>
        /// Gets the layout field collection.
        /// </summary>
        [Description("Layout field collection.")]
        [Browsable(false)]
        [XmlArrayItem(typeof(LayoutField))]
        [DefaultValue(null)]
        public LayoutFieldCollection? Fields
        {
            get
            {
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _fields!)) { return null; }
                if (_fields == null) { _fields = []; }
                return _fields;
            }
        }

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            _fields?.SetSerializeState(serializeState);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{Name} - {Caption}";
        }
    }
}
