namespace Bee.Definition.Layouts
{
    /// <summary>
    /// Interface for a control that binds to a field.
    /// </summary>
    public interface IBindFieldControl
    {
        /// <summary>
        /// Gets or sets the field name.
        /// </summary>
        string FieldName { get; set; }

        /// <summary>
        /// Gets or sets the field value.
        /// </summary>
        object? FieldValue { get; set; }
    }
}
