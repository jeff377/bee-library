using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Base.Collections;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Definition.Collections;
using Bee.Definition.Database;
using Bee.Definition.Layouts;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// A form field definition.
    /// </summary>
    [Description("Form field.")]
    [TreeNode]
    public class FormField : KeyCollectionItem
    {
        private FieldMappingCollection? _relationFieldMappings = null;
        private FieldMappingCollection? _lookupFieldMappings = null;
        private ListItemCollection? _listItems = null;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="FormField"/>.
        /// </summary>
        public FormField()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="FormField"/>.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="caption">The caption text.</param>
        /// <param name="dbType">The database field type.</param>
        public FormField(string fieldName, string caption, FieldDbType dbType)
        {
            FieldName = fieldName;
            Caption = caption;
            DbType = dbType;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FormField"/>.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="caption">The caption text.</param>
        /// <param name="dbType">The database field type.</param>
        /// <param name="type">The field type.</param>
        public FormField(string fieldName, string caption, FieldDbType dbType, FieldType type)
        {
            FieldName = fieldName;
            Caption = caption;
            DbType = dbType;
            Type = type;
        }

        #endregion

        /// <summary>
        /// Gets or sets the field name.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("Field name.")]
        public string FieldName
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// Gets or sets the caption text.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [NotifyParentProperty(true)]
        [Description("Caption text.")]
        public string Caption { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the database field type.
        /// </summary>
        [XmlAttribute]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [Category(PropertyCategories.Data)]
        [Description("Database field type.")]
        public FieldDbType DbType { get; set; } = FieldDbType.String;

        /// <summary>
        /// Gets or sets the field type.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Field type.")]
        [DefaultValue(FieldType.DbField)]
        public FieldType Type { get; set; } = FieldType.DbField;

        /// <summary>
        /// Gets or sets the control type.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Layout)]
        [Description("Control type.")]
        [DefaultValue(ControlType.Auto)]
        public ControlType ControlType { get; set; } = ControlType.Auto;

        /// <summary>
        /// Gets or sets the maximum string length.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Maximum string length.")]
        [DefaultValue(0)]
        public int MaxLength { get; set; } = 0;

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Default value.")]
        [DefaultValue("")]
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display format string.
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [Description("Display format string.")]
        [DefaultValue("")]
        public string DisplayFormat { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number format string.
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [Description("Number format string.")]
        [DefaultValue("")]
        public string NumberFormat { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the numeric semantic kind of this field. Propagated by the layout generator
        /// to <see cref="Layouts.LayoutFieldBase.NumberKind"/>; drives the field's rounding policy and
        /// decimal-places source (see plan-numeric-core.md). The default <see cref="Definition.NumberKind.None"/>
        /// means no numeric handling is applied.
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [Description("Numeric semantic kind driving rounding and decimal places.")]
        [DefaultValue(NumberKind.None)]
        public NumberKind NumberKind { get; set; } = NumberKind.None;

        /// <summary>
        /// Gets or sets the name of the field that holds this amount field's currency code (a SAP
        /// CUKY reference). Applies to <see cref="Definition.NumberKind.Amount"/> fields: when set,
        /// the amount's decimal places resolve from that field's current currency. Empty falls back to
        /// the master document currency (<see cref="FormSchema.CurrencyField"/>), then the company
        /// default currency (see plan-numeric-multicurrency.md §1.4).
        /// </summary>
        [Category(PropertyCategories.Data)]
        [XmlAttribute]
        [Description("Name of the field holding this amount field's currency code (SAP CUKY reference).")]
        [DefaultValue("")]
        public string CurrencyField { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the program ID of the related program.
        /// </summary>
        [XmlAttribute]
        [Category("Relation")]
        [Description("Program ID of the related program.")]
        [DefaultValue("")]
        public string RelationProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets the field mapping collection that maps relation source fields to local fields.
        /// </summary>
        [Category("Relation")]
        [Description("Field mapping collection from the relation source to local fields.")]
        [DefaultValue(null)]
        public FieldMappingCollection? RelationFieldMappings
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _relationFieldMappings!)) { return null; }
                if (_relationFieldMappings == null) { _relationFieldMappings = []; }
                return _relationFieldMappings;
            }
        }

        /// <summary>
        /// Gets or sets the program ID of the UI lookup/selection window.
        /// When a field requires data to be selected from a popup window, set this property to determine which lookup window to open.
        /// </summary>
        [XmlAttribute]
        [Category("Relation")]
        [Description("Program ID of the UI lookup/selection window.")]
        [DefaultValue("")]
        public string LookupProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets the field mapping collection for the lookup/selection window.
        /// Defines how fields returned from the lookup window are mapped back to local fields.
        /// </summary>
        [Category("Relation")]
        [Description("Field mapping collection for the lookup/selection window.")]
        [DefaultValue(null)]
        public FieldMappingCollection? LookupFieldMappings
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _lookupFieldMappings!)) { return null; }
                if (_lookupFieldMappings == null) { _lookupFieldMappings = []; }
                return _lookupFieldMappings;
            }
        }

        /// <summary>
        /// Gets or sets the local fields whose values are displayed in place of this
        /// field's bound value, with multiple fields separated by commas. Used by lookup
        /// editors: the field itself stores a row identifier (Guid), while the editor
        /// shows the mapped display values joined with " - "
        /// (e.g. <c>"ref_dept_id,ref_dept_name"</c> renders as <c>D001 - Engineering</c>).
        /// </summary>
        /// <remarks>
        /// When empty, the resolution falls back to the <see cref="RelationFieldMappings"/>
        /// entries whose source fields are <c>sys_id</c> and <c>sys_name</c> (in that
        /// order, skipping absent mappings) — so a transactional lookup target that only
        /// maps <c>sys_id</c> displays the document number alone.
        /// </remarks>
        [XmlAttribute]
        [Category("Relation")]
        [Description("Local fields displayed in place of the bound value, comma separated (lookup editors).")]
        [DefaultValue("")]
        public string DisplayFields { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this field is visible.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Indicates whether this field is visible.")]
        [DefaultValue(true)]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this field is read-only. Propagated by the
        /// layout generator to <see cref="Layouts.LayoutFieldBase.ReadOnly"/>, so a
        /// <see cref="FormSchema"/> can mark computed or server-derived fields (for example an
        /// order line amount calculated by the business object) non-editable without authoring a
        /// separate <c>FormLayout</c>.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Indicates whether this field is read-only.")]
        [DefaultValue(false)]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this field is required (mandatory input).
        /// Propagated by the layout generator to <see cref="Layouts.LayoutFieldBase.Required"/>,
        /// driving the required caption colour cue without authoring a separate <c>FormLayout</c>.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Indicates whether this field is required (mandatory input).")]
        [DefaultValue(false)]
        public bool Required { get; set; }

        /// <summary>
        /// Gets the list item collection used as the option source shared across layouts.
        /// </summary>
        [Category(PropertyCategories.Data)]
        [Description("List item collection.")]
        [DefaultValue(null)]
        public ListItemCollection? ListItems
        {
            get
            {
                if (SerializationUtilities.IsSerializeEmpty(SerializeState, _listItems!)) { return null; }
                if (_listItems == null) { _listItems = []; }
                return _listItems;
            }
        }

        /// <summary>
        /// Gets or sets the name of a localized <see cref="Language.LanguageEnum"/> used as
        /// this field's dropdown option source. Either a bare name (resolved against the
        /// owning schema's <see cref="FormSchema.ProgId"/> namespace, e.g. <c>"OrderStatus"</c>)
        /// or a fully-qualified <c>"{namespace}.{enumName}"</c> (e.g. <c>"Common.Gender"</c>).
        /// </summary>
        /// <remarks>
        /// When non-empty, <c>FormSchemaLocalizer</c> replaces <see cref="ListItems"/> with
        /// the resolved <see cref="Language.LanguageEnum"/> entries at API delivery time.
        /// Leave empty to keep statically-defined <see cref="ListItems"/> as the option source.
        /// </remarks>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Localized enum name for dropdown options (e.g. 'Common.Gender'); overrides ListItems when set.")]
        [DefaultValue("")]
        public string LangEnumName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the column width. A value greater than 0 is required to take effect.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Column width. A value greater than 0 is required to take effect.")]
        [DefaultValue(0)]
        public int Width { get; set; } = 0;

        /// <summary>
        /// Gets or sets the record-scope role this field plays in permission filtering.
        /// <see cref="Forms.ScopeRole.Owner"/> marks the owner column (resolved by the
        /// <c>Own</c> scope strategy); <see cref="Forms.ScopeRole.Dept"/> marks the department
        /// column (resolved by <c>Dept</c> / <c>DeptAndSub</c>). The default <see cref="Forms.ScopeRole.None"/>
        /// means the field plays no scope role.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Record-scope role for permission filtering (Owner / Dept column).")]
        [DefaultValue(ScopeRole.None)]
        public ScopeRole ScopeRole { get; set; } = ScopeRole.None;

        /// <summary>
        /// Gets the form table that owns this field.
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        [TreeNodeIgnore]
        public FormTable? Table
        {
            get
            {
                if (Collection == null) { return null; }
                return (Collection as FormFieldCollection)?.Owner as FormTable;
            }
        }

        /// <summary>
        /// Resolves the effective display fields for lookup editors. An explicit
        /// <see cref="DisplayFields"/> declaration wins; relation fields fall back to
        /// the <see cref="RelationFieldMappings"/> entries whose source fields are
        /// <c>sys_id</c> and <c>sys_name</c> (in that order, skipping absent mappings).
        /// Returns an empty list when no display field applies; editors join the
        /// resolved values with " - " (a plain space would be ambiguous because
        /// names themselves contain spaces).
        /// </summary>
        public IReadOnlyList<string> GetDisplayFields()
        {
            if (StringUtilities.IsNotEmpty(DisplayFields))
            {
                return DisplayFields.Split(',',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            if (StringUtilities.IsEmpty(RelationProgId)) { return []; }

            var fields = new List<string>();
            foreach (var sourceField in new[] { SysFields.Id, SysFields.Name })
            {
                var mapping = _relationFieldMappings?.FirstOrDefault(
                    m => StringUtilities.IsEquals(m.SourceField, sourceField));
                if (mapping is not null && StringUtilities.IsNotEmpty(mapping.DestinationField))
                    fields.Add(mapping.DestinationField);
            }
            return fields;
        }

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            _relationFieldMappings?.SetSerializeState(serializeState);
            _lookupFieldMappings?.SetSerializeState(serializeState);
            _listItems?.SetSerializeState(serializeState);
        }

        /// <summary>
        /// Creates a deep copy of this instance. The result is unparented (no
        /// owning collection / table) — call sites typically add it to a
        /// <see cref="FormFieldCollection"/> via <c>Add(field.Clone())</c>.
        /// </summary>
        public FormField Clone()
        {
            var copy = new FormField
            {
                FieldName = FieldName,
                Caption = Caption,
                DbType = DbType,
                Type = Type,
                ControlType = ControlType,
                MaxLength = MaxLength,
                DefaultValue = DefaultValue,
                DisplayFormat = DisplayFormat,
                NumberFormat = NumberFormat,
                NumberKind = NumberKind,
                CurrencyField = CurrencyField,
                RelationProgId = RelationProgId,
                LookupProgId = LookupProgId,
                DisplayFields = DisplayFields,
                Visible = Visible,
                ReadOnly = ReadOnly,
                Required = Required,
                Width = Width,
                LangEnumName = LangEnumName,
                ScopeRole = ScopeRole,
            };
            if (_relationFieldMappings != null)
                foreach (var mapping in _relationFieldMappings)
                    copy.RelationFieldMappings!.Add(mapping.Clone());
            if (_lookupFieldMappings != null)
                foreach (var mapping in _lookupFieldMappings)
                    copy.LookupFieldMappings!.Add(mapping.Clone());
            if (_listItems != null)
                foreach (var item in _listItems)
                    copy.ListItems!.Add(item.Clone());
            return copy;
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{FieldName} - {Caption}";
        }
    }
}
