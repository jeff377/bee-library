using System.Data;
using MessagePack;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializable data row containing current and original values, used to support data state and change tracking.
    /// </summary>
    [MessagePackObject]
    public class SerializableDataRow
    {
        /// <summary>
        /// Gets or sets the current values of the row (column name to value mapping).
        /// </summary>
        [Key(0)]
        public Dictionary<string, object?>? CurrentValues { get; set; }

        /// <summary>
        /// Gets or sets the original values of the row (applicable to modified or deleted rows).
        /// </summary>
        [Key(1)]
        public Dictionary<string, object?>? OriginalValues { get; set; }

        /// <summary>
        /// Gets or sets the row state (Added, Modified, Deleted, or Unchanged).
        /// </summary>
        [Key(2)]
        public DataRowState RowState { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableDataRow"/> class and initializes the dictionaries.
        /// </summary>
        public SerializableDataRow()
        {
        }
    }

}
