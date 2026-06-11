namespace Bee.UI.Avalonia.DataObjects
{
    /// <summary>
    /// Event data for <see cref="FormDataObject.FieldValueChanged"/>.
    /// </summary>
    public class FieldValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FieldValueChangedEventArgs"/>.
        /// </summary>
        /// <param name="fieldName">The field (column) name whose value changed.</param>
        /// <param name="value">The new value rendered as a binding string.</param>
        public FieldValueChangedEventArgs(string fieldName, string value)
        {
            FieldName = fieldName;
            Value = value;
        }

        /// <summary>
        /// Gets the field (column) name whose value changed.
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// Gets the new value rendered as a binding string (same format as
        /// <see cref="FormDataObject.GetField"/>).
        /// </summary>
        public string Value { get; }
    }
}
