using MessagePack;

namespace Bee.Api.Core.MessagePack
{
    /// <summary>
    /// Serializable data relation used to describe parent-child relationships between tables.
    /// </summary>
    [MessagePackObject]
    public class SerializableDataRelation
    {
        /// <summary>
        /// Gets or sets the relation name.
        /// </summary>
        [Key(0)]
        public string RelationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parent table name.
        /// </summary>
        [Key(1)]
        public string ParentTable { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the child table name.
        /// </summary>
        [Key(2)]
        public string ChildTable { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the column names of the parent table that form the relation key.
        /// </summary>
        [Key(3)]
        public List<string> ParentColumns { get; set; }

        /// <summary>
        /// Gets or sets the column names of the child table that form the relation key.
        /// </summary>
        [Key(4)]
        public List<string> ChildColumns { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableDataRelation"/> class and initializes the column collections.
        /// </summary>
        public SerializableDataRelation()
        {
            ParentColumns = new List<string>();
            ChildColumns = new List<string>();
        }
    }

}
