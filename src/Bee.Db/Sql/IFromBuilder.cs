namespace Bee.Db.Sql
{
    /// <summary>
    /// Defines the interface for building a SQL FROM clause.
    /// </summary>
    public interface IFromBuilder
    {
        /// <summary>
        /// Builds the FROM clause, including any JOIN statements.
        /// </summary>
        /// <param name="mainTableName">The main table name.</param>
        /// <param name="joins">The collection of table JOIN relationships.</param>
        /// <returns>The FROM clause string.</returns>
        string Build(string mainTableName, TableJoinCollection joins);
    }
}
