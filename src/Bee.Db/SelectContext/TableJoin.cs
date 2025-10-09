using System.Collections.Generic;
using Bee.Base;

namespace Bee.Db
{
    /// <summary>
    /// 描述兩個資料表之間的 Join 關係。
    /// </summary>
    public class TableJoin : KeyCollectionItem
    {
        /// <summary>
        /// 鍵值，記錄建立 Join 關係的參考來源的唯一鍵值。
        /// </summary>
        public override string Key
        {
            get { return base.Key; }
            set { base.Key = value; }
        }

        /// <summary>
        /// Join 類型。
        /// </summary>
        public JoinType JoinType { get; set; }

        /// <summary>
        /// 左側資料表名稱。
        /// </summary>
        public string LeftTable { get; set; }

        /// <summary>
        /// 左側資料表別名。
        /// </summary>
        public string LeftAlias { get; set; }

        /// <summary>
        /// 右側資料表名稱。
        /// </summary>
        public string RightTable { get; set; }

        /// <summary>
        /// 右側資料表別名。
        /// </summary>
        public string RightAlias { get; set; }

        /// <summary>
        /// Join 條件清單。
        /// </summary>
        public List<JoinCondition> Conditions { get; set; } = new List<JoinCondition>();

        /// <summary>
        /// 轉換為 SQL JOIN 語法。
        /// </summary>
        /// <returns>完整的 JOIN 子句字串。</returns>
        public string ToSql()
        {
            var joinKeyword = JoinType.ToString().ToUpperInvariant() + " JOIN";
            var conditionSql = string.Join(" AND ", Conditions.ConvertAll(c => c.ToSql()));
            return $"{joinKeyword} {RightTable} {RightAlias} ON {conditionSql}";
        }
    }

}
