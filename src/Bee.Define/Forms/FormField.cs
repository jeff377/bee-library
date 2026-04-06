using Bee.Base;
using Bee.Base.Attributes;
using Bee.Base.Data;
using Bee.Base.Serialization;
using Bee.Base.Collections;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Bee.Define.Forms
{
    /// <summary>
    /// A form field definition.
    /// </summary>
    [Serializable]
    [XmlType("FormField")]
    [Description("Form field.")]
    [TreeNode]
    public class FormField : KeyCollectionItem
    {
        private FieldMappingCollection _relationFieldMappings = null;
        private FieldMappingCollection _lookupFieldMappings = null;

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
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
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
        public FieldMappingCollection RelationFieldMappings
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _relationFieldMappings)) { return null; }
                if (_relationFieldMappings == null) { _relationFieldMappings = new FieldMappingCollection(); }
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
        public FieldMappingCollection LookupFieldMappings
        {
            get
            {
                // Return null if the collection is empty during serialization
                if (BaseFunc.IsSerializeEmpty(SerializeState, _lookupFieldMappings)) { return null; }
                if (_lookupFieldMappings == null) { _lookupFieldMappings = new FieldMappingCollection(); }
                return _lookupFieldMappings;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this field is visible.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Indicates whether this field is visible.")]
        [DefaultValue(true)]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets the column width. A value greater than 0 is required to take effect.
        /// </summary>
        [Category(PropertyCategories.Layout)]
        [XmlAttribute]
        [Description("Column width. A value greater than 0 is required to take effect.")]
        [DefaultValue(0)]
        public int Width { get; set; } = 0;

        /// <summary>
        /// Gets the form table that owns this field.
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        [TreeNodeIgnore]
        public FormTable Table
        {
            get
            {
                if (Collection == null) { return null; }
                return (Collection as FormFieldCollection).Owner as FormTable;
            }
        }

        /// <summary>
        /// Sets the serialization state.
        /// </summary>
        /// <param name="serializeState">The serialization state.</param>
        public override void SetSerializeState(SerializeState serializeState)
        {
            base.SetSerializeState(serializeState);
            BaseFunc.SetSerializeState(_relationFieldMappings, serializeState);
            BaseFunc.SetSerializeState(_lookupFieldMappings, serializeState);
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
