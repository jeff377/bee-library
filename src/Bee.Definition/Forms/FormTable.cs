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
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _fields!)) { return null; }
                if (_fields == null) { _fields = new FormFieldCollection(this); }
                return _fields;
            }
        }

        /// <summary>
        /// Gets every field marked <see cref="ScopeRole.Owner"/> (resolved by the <c>Own</c>
        /// record-scope strategy). A master table may mark more than one owner column — for example a
        /// form whose creator and a co-owner should both see the record — and the scope predicate
        /// OR-unions them. Returns an empty list when no field carries the role.
        /// </summary>
        public IReadOnlyList<FormField> GetOwnerFields() => FindScopeFields(ScopeRole.Owner);

        /// <summary>
        /// Gets every field marked <see cref="ScopeRole.Dept"/> (resolved by the <c>Dept</c> /
        /// <c>DeptAndSub</c> record-scope strategies). A master table may mark more than one department
        /// column — for example a transfer form's from-department and to-department, so both
        /// departments' managers can see the record — and the scope predicate OR-unions them. Returns
        /// an empty list when no field carries the role.
        /// </summary>
        public IReadOnlyList<FormField> GetDeptFields() => FindScopeFields(ScopeRole.Dept);

        /// <summary>
        /// Gets the first field marked <see cref="ScopeRole.Owner"/>, or <c>null</c> when none.
        /// Prefer <see cref="GetOwnerFields"/>; this convenience returns only the first of possibly many.
        /// </summary>
        public FormField? GetOwnerField() => FindScopeField(ScopeRole.Owner);

        /// <summary>
        /// Gets the first field marked <see cref="ScopeRole.Dept"/>, or <c>null</c> when none.
        /// Prefer <see cref="GetDeptFields"/>; this convenience returns only the first of possibly many.
        /// </summary>
        public FormField? GetDeptField() => FindScopeField(ScopeRole.Dept);

        private FormField? FindScopeField(ScopeRole role)
        {
            if (Fields == null) { return null; }
            foreach (var field in Fields)
            {
                if (field.ScopeRole == role) { return field; }
            }
            return null;
        }

        private IReadOnlyList<FormField> FindScopeFields(ScopeRole role)
        {
            if (Fields == null) { return []; }
            var list = new List<FormField>();
            foreach (var field in Fields)
            {
                if (field.ScopeRole == role) { list.Add(field); }
            }
            return list;
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
                    StringUtilities.IsEmpty(field.RelationProgId) ||
                    ValueUtilities.IsEmpty(field.RelationFieldMappings!))
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
            _fields?.SetSerializeState(serializeState);
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

        /// <summary>
        /// Creates a deep copy of this instance. The result is unparented (no
        /// owning collection / schema) — typically added to a
        /// <see cref="FormTableCollection"/> via <c>Add(table.Clone())</c>.
        /// </summary>
        /// <remarks>
        /// <see cref="RelationFieldReferences"/> is derived state and is not
        /// copied; the clone will rebuild it lazily on first access from the
        /// cloned <see cref="Fields"/>.
        /// </remarks>
        public FormTable Clone()
        {
            var copy = new FormTable(TableName, DisplayName)
            {
                DbTableName = DbTableName,
            };
            if (_fields != null)
                foreach (var field in _fields)
                    copy.Fields!.Add(field.Clone());
            return copy;
        }
    }
}
