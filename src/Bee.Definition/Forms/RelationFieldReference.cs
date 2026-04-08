using Bee.Core;
using Bee.Core.Collections;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// Records the reference source of a relation field.
    /// </summary>
    public class RelationFieldReference : KeyCollectionItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RelationFieldReference"/>.
        /// </summary>
        public RelationFieldReference() { }

        /// <summary>
        /// Initializes a new instance of <see cref="RelationFieldReference"/>.
        /// </summary>
        /// <param name="fieldName">The name of the relation field.</param>
        /// <param name="foreignKeyField">The foreign key field.</param>
        /// <param name="sourceProgId">The program ID of the relation source.</param>
        /// <param name="sourceField">The field name of the relation source.</param>
        public RelationFieldReference(string fieldName, FormField foreignKeyField, string sourceProgId, string sourceField)
        {
            FieldName = fieldName;
            ForeignKeyField = foreignKeyField;
            SourceProgId = sourceProgId;
            SourceField = sourceField;
        }

        /// <summary>
        /// Gets or sets the name of the relation field.
        /// </summary>
        public string FieldName
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// Gets or sets the foreign key field.
        /// </summary>
        public FormField ForeignKeyField { get; set; }

        /// <summary>
        /// Gets or sets the program ID of the relation source.
        /// </summary>
        public string SourceProgId { get; set; }

        /// <summary>
        /// Gets or sets the field name of the relation source.
        /// </summary>
        public string SourceField { get; set; }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{SourceProgId}.{SourceField} -> {FieldName}";
        }
    }
}
