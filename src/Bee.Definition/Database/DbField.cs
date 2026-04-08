using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Bee.Core;
using Bee.Core.Attributes;
using Bee.Core.Data;
using Bee.Core.Collections;
using Newtonsoft.Json;

namespace Bee.Definition.Database
{
    /// <summary>
    /// Database field schema.
    /// </summary>
    [Serializable]
    [XmlType("DbField")]
    [Description("Database field schema.")]
    [TreeNode]
    public class DbField : KeyCollectionItem, IDefineField
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DbField"/>.
        /// </summary>
        public DbField()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="DbField"/>.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="caption">The caption text.</param>
        /// <param name="dbType">The field database type.</param>
        public DbField(string fieldName, string caption, FieldDbType dbType)
        {
            FieldName = fieldName;
            Caption = caption;
            DbType = dbType;
        }

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
        /// Gets or sets the database data type.
        /// </summary>
        [XmlAttribute]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        [Category(PropertyCategories.Data)]
        [Description("Database data type.")]
        public FieldDbType DbType { get; set; } = FieldDbType.String;

        /// <summary>
        /// Gets or sets the field length for string-type fields; applies to <see cref="FieldDbType.String"/>.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Field length for string-type fields; applies to String.")]
        [DefaultValue(0)]
        public int Length { get; set; } = 0;

        /// <summary>
        /// Gets or sets the numeric precision (total digits) for Decimal-type fields.
        /// </summary>
        /// <remarks>
        /// Applies to the Decimal type; represents the total number of digits including both integer and fractional parts.
        /// For example: Precision=19, Scale=4 represents DECIMAL(19,4), with a range of -999999999999999.9999 to 999999999999999.9999.
        /// </remarks>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Numeric precision (total digits) for Decimal-type fields.")]
        [DefaultValue(18)]
        public int Precision { get; set; } = 18;

        /// <summary>
        /// Gets or sets the number of decimal places for Decimal-type fields.
        /// </summary>
        /// <remarks>
        /// Applies to the Decimal type; represents the number of digits after the decimal point.
        /// For example: Precision=19, Scale=4 means up to 4 decimal places.
        /// </remarks>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Number of decimal places for Decimal-type fields.")]
        [DefaultValue(0)]
        public int Scale { get; set; } = 0;


        /// <summary>
        /// Gets or sets a value indicating whether null values are allowed.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Indicates whether null values are allowed.")]
        [DefaultValue(false)]
        public bool AllowNull { get; set; } = false;

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        [XmlAttribute]
        [Category(PropertyCategories.Data)]
        [Description("Default value.")]
        [DefaultValue("")]
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the schema upgrade action used at runtime for field schema comparison. This property is not serialized.
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        [DefaultValue(DbUpgradeAction.None)]
        public DbUpgradeAction UpgradeAction { get; set; } = DbUpgradeAction.None;

        /// <summary>
        /// Creates a copy of this instance.
        /// </summary>
        public DbField Clone()
        {
            return new DbField
            {
                FieldName = FieldName,
                Caption = Caption,
                DbType = DbType,
                Length = Length,
                Precision = Precision,
                Scale = Scale,
                AllowNull = AllowNull,
                DefaultValue = DefaultValue
            };
        }

        /// <summary>
        /// Compares whether the schema is identical to another instance.
        /// </summary>
        /// <param name="source">The source object to compare against.</param>
        public bool Compare(DbField source)
        {
            // Compare data type
            if (DbType != source.DbType) { return false; }
            // Compare AllowNull
            if (AllowNull != source.AllowNull) { return false; }
            // Compare field length for String type
            if ((DbType == FieldDbType.String) && (Length != source.Length))
                return false;
            // Compare precision and scale for Decimal type
            if ((DbType == FieldDbType.Decimal) && (Precision != source.Precision || Scale != source.Scale))
                return false;
            // Compare default value
            if (!StrFunc.IsEquals(DefaultValue, source.DefaultValue)) { return false; }

            return true;
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
