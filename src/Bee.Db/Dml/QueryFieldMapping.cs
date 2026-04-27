using Bee.Base.Collections;

namespace Bee.Db.Dml
{
    /// <summary>
    /// Describes the mapping between a query field and its original data source.
    /// Query fields include those used in Select, Where, and Order By clauses.
    /// </summary>
    public class QueryFieldMapping : KeyCollectionItem
    {
        /// <summary>
        /// Gets or sets the field name used in the query.
        /// </summary>
        public string FieldName
        {
            get { return this.Key; }
            set { this.Key = value; }
        }

        /// <summary>
        /// Gets or sets the source table alias.
        /// </summary>
        public string SourceAlias { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source table column name.
        /// </summary>
        public string SourceField { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the JOIN relationship associated with this query field.
        /// </summary>
        public TableJoin? TableJoin { get; set; }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"{SourceAlias}.{SourceField} AS {FieldName}";
        }
    }
}
