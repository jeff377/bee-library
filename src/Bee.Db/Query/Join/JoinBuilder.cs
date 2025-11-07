using Bee.Define;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bee.Db
{
    /// <summary>
    /// ORDER BY 子句建置器。
    /// </summary>
    public class JoinBuilder
    {
        private readonly DatabaseType _databaseType;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="databaseType">資料庫類型。</param>
        public JoinBuilder(DatabaseType databaseType)
        {
            _databaseType = databaseType;
        }

        /// <summary>
        /// 建立 JOIN 子句。
        /// </summary>
        /// <param name="joins">資料表 Join 關係集合。</param>
        /// <returns>JOIN 子句字串。</returns>
        public string Build(TableJoinCollection joins)
        {
            if (joins == null || joins.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
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
