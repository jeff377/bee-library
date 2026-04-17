using Bee.Base.Data;
using MessagePack;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializable column definition used to describe DataColumn properties.
    /// </summary>
    [MessagePackObject]
    public class SerializableDataColumn
    {
        /// <summary>
        /// Gets or sets the column name.
        /// </summary>
        [Key(0)]
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data type as a <see cref="FieldDbType"/> enum value.
        /// </summary>
        [Key(1)]
        public FieldDbType DataType { get; set; }

        /// <summary>
        /// Gets or sets the display name (Caption).
        /// </summary>
        [Key(2)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether null values are allowed.
        /// </summary>
        [Key(3)]
        public bool AllowDBNull { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is read-only.
        /// </summary>
        [Key(4)]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the maximum column length (applicable to string types only).
        /// </summary>
        [Key(5)]
        public int MaxLength { get; set; }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        [Key(6)]
        public object? DefaultValue { get; set; }
    }

}
