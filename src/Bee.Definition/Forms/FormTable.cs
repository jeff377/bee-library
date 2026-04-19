using Bee.Definition.Database;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Serialization;
using Bee.Base.Collections;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// A form table definition.
    /// </summary>
    [Serializable]
    [XmlType("FormTable")]
    [Description("Form table.")]
    [TreeNode]
    public class FormTable : KeyCollectionItem
    {
        private FormFieldCollection? _fields = null;
        private RelationFieldReferenceCollection? _relationFieldReferences = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="FormTable"/>.
        /// </summary>
        public FormTable()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="FormTable"/>.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="displayName">The display name.</param>
        public FormTable(string tableName, string displayName)
        {
            TableName = tableName;
            DisplayName = displayName;
        }

        #endregion

        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("Table name.")]
        public string TableName
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// Gets or sets the database table name.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Database table name.")]
        public string DbTableName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("Display name.")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the field collection.
        /// </summary>
        [Description("Field collection.")]
        [DefaultValue(null)]
        public FormFieldCollection? Fields
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _fields!)) { return null; }
                if (_fields == null) { _fields = new FormFieldCollection(this); }
                return _fields;
            }
        }

        /// <summary>
        /// Gets the relation field reference collection.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public RelationFieldReferenceCollection RelationFieldReferences
        {
            get
            {
                if (_relationFieldReferences == null)
                    _relationFieldReferences = CreateRelationFieldReferences();
                return _relationFieldReferences;
            }
        }

        /// <summary>
        /// Creates the relation field reference collection.
        /// </summary>
        private RelationFieldReferenceCollection CreateRelationFieldReferences()
        {
            var references = new RelationFieldReferenceCollection();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in Fields!)
            {
                if (field.Type != FieldType.DbField ||
                    StrFunc.IsEmpty(field.RelationProgId) ||
                    BaseFunc.IsEmpty(field.RelationFieldMappings!))
                    continue;

                foreach (var mapping in field.RelationFieldMappings!)
                {
                    string destField = mapping.DestinationField;
                    if (!Fields.Contains(destField))
                        throw new KeyNotFoundException($"DestinationField '{destField}' does not exist in the form field collection.");
                    if (!seen.Add(destField))
                        throw new InvalidOperationException($"DestinationField '{destField}' has duplicate data in RelationFieldReferences.");

                    references.Add(new RelationFieldReference(destField, field, field.RelationProgId, mapping.SourceField));
                }
            }

            return references;
        }

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_fields!, serializeState);
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{TableName} - {DisplayName}";
        }

        /// <summary>
        /// Generates a database table schema from this form table.
        /// </summary>
        /// <returns>The generated table schema.</returns>
        public TableSchema GenerateDbTable()
        {
            return TableSchemaGenerator.Generate(this);
        }
    }
}
