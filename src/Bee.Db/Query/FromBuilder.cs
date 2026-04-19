using Bee.Definition;
using System.Text;

namespace Bee.Db.Query
{
    /// <summary>
    /// Builds the SQL FROM clause, including any JOIN statements.
    /// </summary>
    public class FromBuilder : IFromBuilder
    {
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// Initializes a new instance of <see cref="FromBuilder"/>.
        /// </summary>
        /// <param name="databaseType">The database type.</param>
        public FromBuilder(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        /// <summary>
        /// Builds the FROM clause, including any JOIN statements.
        /// </summary>
        /// <param name="mainTableName">The main table name.</param>
        /// <param name="joins">The collection of table JOIN relationships.</param>
        /// <returns>The FROM clause string.</returns>
        public string Build(string mainTableName, TableJoinCollection joins)
        {
            var sb = new StringBuilder();
            sb.Append($"FROM {QuoteIdentifier(mainTableName)} A");

            if (joins == null || joins.Count == 0)
                return sb.ToString();

            var joinList = joins.OrderBy(j => j.RightAlias);
            foreach (var join in joinList)
            {
                var joinKeyword = join.JoinType.ToString().ToUpperInvariant() + " JOIN";
                sb.AppendLine();
                sb.Append($"{joinKeyword} {QuoteIdentifier(join.RightTable)} {join.RightAlias} ON {join.LeftAlias}.{QuoteIdentifier(join.LeftField)} = {join.RightAlias}.{QuoteIdentifier(join.RightField)}");
            }
            return sb.ToString();
        }

        private string QuoteIdentifier(string identifier)
        {
            return DbFunc.QuoteIdentifier(_databaseType, identifier);
        }
    }
}
