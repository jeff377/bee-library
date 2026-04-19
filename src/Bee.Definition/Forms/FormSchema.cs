using Bee.Definition.Layouts;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using System.Text.Json.Serialization;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// Form schema definition.
    /// </summary>
    [Serializable]
    [XmlType("FormSchema")]
    [Description("Form schema definition.")]
    [TreeNode("Form Schema")]
    public class FormSchema : IObjectSerializeFile
    {
        private FormTableCollection? _tables = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="FormSchema"/>.
        /// </summary>
        public FormSchema()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FormSchema"/>.
        /// </summary>
        /// <param name="progId">The program ID.</param>
        /// <param name="displayName">The display name.</param>
        public FormSchema(string progId, string displayName)
        {
            ProgId= progId;
            DisplayName = displayName;
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
            BaseFunc.SetSerializeState(_tables!, serializeState);
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
        /// Gets the time at which this object was created.
        /// </summary>
        [XmlIgnore, JsonIgnore]
        [Browsable(false)]
        public DateTime CreateTime { get; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the program ID.
        /// </summary>
        [XmlAttribute()]
        [Description("Program ID.")]
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Display name.")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list field collection string, with multiple fields separated by commas.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("List field collection string, with multiple fields separated by commas.")]
        public string ListFields { get; set; } = string.Empty;

        /// <summary>
        /// Gets the table collection.
        /// </summary>
        [Description("Table collection.")]
        [DefaultValue(null)]
        public FormTableCollection? Tables
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _tables!)) { return null; }
                if (_tables == null) { _tables = new FormTableCollection(this); }
                return _tables;
            }
        }

        /// <summary>
        /// Gets the master table.
        /// </summary>
        [Browsable(false)]
        [TreeNodeIgnore]
        public FormTable? MasterTable
        {
            get
            {
                if (StrFunc.IsEmpty(this.ProgId) || !this.Tables!.Contains(this.ProgId))
                    return null;
                else
                    return this.Tables![this.ProgId];
            }
        }

        /// <summary>
        /// Gets the list layout for this form schema.
        /// </summary>
        public LayoutGrid GetListLayout()
        {
            return DefineFunc.GetListLayout(this);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{this.ProgId} - {this.DisplayName}";
        }
    }
}
