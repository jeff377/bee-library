using System.ComponentModel;
using Bee.Base.Attributes;
using Bee.Base.Data;
using Bee.Base.Collections;

namespace Bee.Definition.Forms
{
    /// <summary>
    /// A collection of form fields.
    /// </summary>
    [Serializable]
    [Description("Form field collection.")]
    [TreeNode("Fields", true)]
    public class FormFieldCollection : KeyCollectionBase<FormField>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FormFieldCollection"/>.
        /// </summary>
        /// <param name="formTable">The owning form table.</param>
        public FormFieldCollection(FormTable formTable) : base(formTable)
        { }

        /// <summary>
        /// Adds a field to the collection.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="caption">The caption text.</param>
        /// <param name="dbType">The database field type.</param>
        public FormField Add(string fieldName, string caption, FieldDbType dbType)
        {
            var field = new FormField(fieldName, caption, dbType);
            base.Add(field);
            return field;
        }

        /// <summary>
        /// Adds a string field to the collection.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="caption">The caption text.</param>
        /// <param name="maxLength">The maximum string length.</param>
        public FormField AddStringField(string fieldName, string caption, int maxLength)
        {
            var field = new FormField(fieldName, caption, FieldDbType.String);
            field.MaxLength = maxLength;
            base.Add(field);
            return field;
        }
    }
}
