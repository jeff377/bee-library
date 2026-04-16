namespace Bee.Db.Query
{
    /// <summary>
    /// Represents the field source mappings and table JOIN relationships required for a SQL query.
    /// This class records all field mappings (used in Select, Where, and Order By clauses) and the associated
    /// TableJoin settings needed to compose a complete and correct SQL query.
    /// </summary>
    public class SelectContext
    {
        /// <summary>
        /// Gets or sets all field source mappings used by the query.
        /// Each entry describes a query field's mapping to its source table, column, and JOIN relationship.
        /// </summary>
        public QueryFieldMappingCollection FieldMappings { get; set; } = new QueryFieldMappingCollection();

        /// <summary>
        /// Gets or sets all table JOIN relationships required by the query.
        /// Records JOIN conditions, structures, and aliases between tables.
        /// </summary>
        public TableJoinCollection Joins { get; set; } = new TableJoinCollection();
    }
}
