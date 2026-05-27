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
            _tables?.SetSerializeState(serializeState);
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
        /// Gets or sets the database category id (required).
        /// Determines which <see cref="Settings.DbCategory"/> the tables in this schema
        /// belong to, and thus where their generated <see cref="Database.TableSchema"/>
        /// files are persisted.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Database category id (required).")]
        public string CategoryId { get; set; } = string.Empty;

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
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _tables!)) { return null; }
                if (_tables == null) { _tables = new FormTableCollection(this); }
                return _tables;
            }
        }

        /// <summary>
        /// Gets the master table.
        /// </summary>
        /// <remarks>
        /// Excluded from JSON serialization because the value is always equal to
        /// <c>Tables[ProgId]</c>; emitting it would duplicate the master table
        /// payload for JSON consumers (notably the JS Plain wire format).
        /// </remarks>
        [Browsable(false)]
        [TreeNodeIgnore]
        [JsonIgnore]
        public FormTable? MasterTable
        {
            get
            {
                if (StringUtilities.IsEmpty(this.ProgId) || !this.Tables!.Contains(this.ProgId))
                    return null;
                else
                    return this.Tables![this.ProgId];
            }
        }

        /// <summary>
        /// Gets the form layout for this schema.
        /// </summary>
        /// <param name="layoutId">The layout ID to assign to the generated layout.</param>
        public FormLayout GetFormLayout(string layoutId = "default")
            => FormLayoutGenerator.Generate(this, layoutId);

        /// <summary>
        /// Gets the list layout for this form schema.
        /// </summary>
        public LayoutGrid GetListLayout()
            => ListLayoutGenerator.Generate(this);

        /// <summary>
        /// Creates a deep copy of this instance. Use this whenever a per-session
        /// view of a cached <see cref="FormSchema"/> must be mutated (e.g. before
        /// applying language-specific localization).
        /// </summary>
        /// <remarks>
        /// Cached <see cref="FormSchema"/> instances returned by
        /// <see cref="Storage.IDefineAccess.GetFormSchema"/> are shared across
        /// every session in the process — see <c>docs/development-constraints.md</c>
        /// § <i>Definition Data Immutability After Init</i>. Mutating without
        /// cloning first leaks state across sessions and races under concurrency.
        /// </remarks>
        public FormSchema Clone()
        {
            var copy = new FormSchema(ProgId, DisplayName)
            {
                CategoryId = CategoryId,
                ListFields = ListFields,
            };
            if (_tables != null)
                foreach (var table in _tables)
                    copy.Tables!.Add(table.Clone());
            return copy;
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
