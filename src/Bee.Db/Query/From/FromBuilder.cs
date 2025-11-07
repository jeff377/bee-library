using Bee.Define;
using System.Linq;
using System.Text;

namespace Bee.Db
{
    /// <summary>
    /// FROM 子句建置器。
    /// </summary>
    public class FromBuilder : IFromBuilder
    {
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        public FromBuilder(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        /// <summary>
        /// 建立 FROM 子句。
        /// </summary>
        /// <param name="mainTableName">主資料表名稱。</param>
        /// <param name="joins">資料表 Join 關係集合。</param>
        /// <returns>JOIN 子句字串。</returns>
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
                sb.AppendLine($"{joinKeyword} {QuoteIdentifier(join.RightTable)} {join.RightAlias} ON {join.LeftAlias}.{QuoteIdentifier(join.LeftField)} = {join.RightAlias}.{QuoteIdentifier(join.RightField)}");
            }
            return sb.ToString();
        }

        private string QuoteIdentifier(string identifier)
        {
            return DbFunc.QuoteIdentifier(_databaseType, identifier);
        }
    }
}
