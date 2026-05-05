using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Bee.Base.Attributes;
using Bee.Base.Serialization;

namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Form layout configuration.
    /// Holds the master sections plus 0..N detail grids for a single form view.
    /// </summary>
    [Description("Form layout configuration.")]
    [TreeNode]
    public class FormLayout : IObjectSerializeFile
    {
        private LayoutSectionCollection? _sections = null;
        private LayoutGridCollection? _details = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="FormLayout"/>.
        /// </summary>
        public FormLayout()
        {
        }

        #endregion

        #region IObjectSerializeFile Interface

        /// <summary>
        /// Gets the serialization state.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public SerializeState SerializeState { get; private set; } = SerializeState.None;

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public void SetSerializeState(SerializeState serializeState)
        {
            SerializeState = serializeState;
            _sections?.SetSerializeState(serializeState);
            _details?.SetSerializeState(serializeState);
        }

        /// <summary>
        /// Gets the file path bound to serialization.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public string ObjectFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// Sets the file path bound to serialization.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public void SetObjectFilePath(string filePath)
        {
            ObjectFilePath = filePath;
        }

        #endregion

        /// <summary>
        /// Gets or sets the form layout ID.
        /// Identifies this layout among multiple layouts that may share the same <see cref="ProgId"/>.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Form layout ID.")]
        public string LayoutId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the program ID this layout belongs to (also locates the master table).
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Program ID this layout belongs to.")]
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the caption text for this layout.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Caption text.")]
        [DefaultValue("")]
        public string Caption { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum number of columns for the master form area.
        /// All sections share this column division.
        /// In WinForm rendering this is treated as a fixed column count;
        /// in responsive web rendering this is the upper bound and may shrink on narrow viewports.
        /// </summary>
        [XmlAttribute]
        [NotifyParentProperty(true)]
        [Description("Maximum number of columns for the master form area.")]
        [DefaultValue(2)]
        public int ColumnCount { get; set; } = 2;

        /// <summary>
        /// Gets the master section collection.
        /// </summary>
        [Description("Master section collection.")]
        [Browsable(false)]
        [XmlArray("Sections")]
        [XmlArrayItem(typeof(LayoutSection))]
        [DefaultValue(null)]
        public LayoutSectionCollection? Sections
        {
            get
            {
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _sections!)) { return null; }
                if (_sections == null) { _sections = []; }
                return _sections;
            }
        }

        /// <summary>
        /// Gets the detail grid collection.
        /// Detail grids always render full-width below the master sections.
        /// </summary>
        [Description("Detail grid collection.")]
        [Browsable(false)]
        [XmlArray("Details")]
        [XmlArrayItem(typeof(LayoutGrid))]
        [DefaultValue(null)]
        public LayoutGridCollection? Details
        {
            get
            {
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _details!)) { return null; }
                if (_details == null) { _details = []; }
                return _details;
            }
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{LayoutId} - {Caption}";
        }
    }
}
