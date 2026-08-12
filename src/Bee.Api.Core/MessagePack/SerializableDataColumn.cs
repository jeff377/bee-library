using Bee.Base.Data;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializable column definition used to describe DataColumn properties.
    /// </summary>
    internal class SerializableDataColumn
    {
        /// <summary>
        /// Gets or sets the column name.
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data type as a <see cref="FieldDbType"/> enum value.
        /// </summary>
        public FieldDbType DataType { get; set; }

        /// <summary>
        /// Gets or sets the display name (Caption).
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether null values are allowed.
        /// </summary>
        public bool AllowDBNull { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is read-only.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the maximum column length (applicable to string types only).
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        public object? DefaultValue { get; set; }
    }

}
