using Bee.Definition.Collections;
using MessagePack;

namespace Bee.Definition
{
    /// <summary>
    /// A sort field definition.
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public sealed class SortField : MessagePackCollectionItem
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SortField"/>.
        /// </summary>
        public SortField()
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="SortField"/>.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="direction">The sort direction.</param>
        public SortField(string fieldName, SortDirection direction)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field cannot be null or empty.", nameof(fieldName));

            FieldName = fieldName;
            Direction = direction;
        }

        /// <summary>
        /// Gets or sets the field name or SQL expression.
        /// </summary>
        [Key(100)]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sort direction.
        /// </summary>
        [Key(101)]
        public SortDirection Direction { get; set; }
    }
}
